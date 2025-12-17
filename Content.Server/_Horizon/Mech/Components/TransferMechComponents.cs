using Content.Server.Mech.Systems;
using Content.Shared.Construction;
using Content.Shared.Mech.Components;
using JetBrains.Annotations;
using Content.Shared.Storage;

namespace Content.Server._Horizon.Mech.Components
{
    /// <summary>
    /// Действие для передачи батареи и модулей меха при улучшении.
    /// Используется в графах конструкции при улучшении меха, чтобы
    /// установленная батарея и модули переносились в новый мех.
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class TransferMechComponents : IGraphAction
    {
        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            if (!entityManager.HasComponent<MechComponent>(uid))
                return;

            //Logger.Debug($"TransferMechComponents: начало обработки меха {entityManager.ToPrettyString(uid)}");

            var mechSystem = entityManager.EntitySysManager.GetEntitySystem<MechSystem>();
            var mechComp = entityManager.GetComponent<MechComponent>(uid);

            // Извлекаем пилота
            if (mechComp.PilotSlot.ContainedEntity != null)
            {
                var pilot = mechComp.PilotSlot.ContainedEntity.Value;
                //Logger.Debug($"TransferMechComponents: извлекаем пилота {entityManager.ToPrettyString(pilot)}");
                mechSystem.TryEject(uid, mechComp);

                // Отправляем событие с признаком, что это пилот
                entityManager.EventBus.RaiseEvent(EventSource.Local, new MechComponentsTransferEvent(pilot, true));
            }

            var components = new List<EntityUid>();

            // Батарея
            if (mechComp.BatterySlot.ContainedEntity != null)
            {
                var battery = mechComp.BatterySlot.ContainedEntity.Value;
                components.Add(battery);
                //Logger.Debug($"TransferMechComponents: добавляем батарею {entityManager.ToPrettyString(battery)} для переноса");
            }

            // Оборудование
            if (mechComp.EquipmentContainer.ContainedEntities.Count > 0)
            {
                foreach (var equipment in mechComp.EquipmentContainer.ContainedEntities)
                {
                    components.Add(equipment);
                    //Logger.Debug($"TransferMechComponents: добавляем оборудование {entityManager.ToPrettyString(equipment)} для переноса");
                }
            }

            // Проверяем наличие хранилища
            if (entityManager.TryGetComponent<StorageComponent>(uid, out var storageComp))
            {
                //Logger.Debug($"TransferMechComponents: проверяем содержимое хранилища в мехе {entityManager.ToPrettyString(uid)}");

                if (storageComp.Container.ContainedEntities.Count > 0)
                {
                    // Копируем список, чтобы избежать проблем при изменении коллекции
                    var storedItems = new List<EntityUid>(storageComp.Container.ContainedEntities);

                    foreach (var item in storedItems)
                    {
                        // Отправляем событие для переноса в новый мех (с хранилищем в новом мехе разберётся OnMechComponentsTransfer)
                        entityManager.EventBus.RaiseEvent(EventSource.Local, new MechComponentsTransferEvent(item, false, true));
                        //Logger.Debug($"TransferMechComponents: добавляем предмет из хранилища {entityManager.ToPrettyString(item)} для переноса");
                    }
                }
            }

            // Отправляем события для каждого компонента
            foreach (var component in components)
            {
                if (entityManager.EntityExists(component))
                {
                    entityManager.EventBus.RaiseEvent(EventSource.Local, new MechComponentsTransferEvent(component));
                    //Logger.Debug($"TransferMechComponents: отправляем событие для {entityManager.ToPrettyString(component)}");
                }
                //else
                //{
                //    Logger.Warning($"TransferMechComponents: компонент не существует {component}");
                //}
            }

            //Logger.Debug($"TransferMechComponents: ставим в очередь удаление меха {entityManager.ToPrettyString(uid)}");
            entityManager.QueueDeleteEntity(uid);
        }
    }

    public sealed class MechComponentsTransferEvent : EntityEventArgs
    {
        public EntityUid Component;
        public bool IsPilot = false;
        public bool IsStorageItem = false;

        public MechComponentsTransferEvent(EntityUid component, bool isPilot = false, bool isStorageItem = false)
        {
            Component = component;
            IsPilot = isPilot;
            IsStorageItem = isStorageItem;
        }
    }
}