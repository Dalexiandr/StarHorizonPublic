using System.Collections.Generic;
using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Tag;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;

namespace Content.Server._Horizon.Administration;

[AdminCommand(AdminFlags.Admin)]
public sealed class EntityByTagCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private static readonly ISawmill _sawmill = Logger.GetSawmill("entityByTag");

    public string Command => "entityByTag";
    public string Description => "Lists all entities that have the specified exact tag on the server at the moment.";
    public string Help => "Usage: entityByTag <tagName> - outputs all entities with the exact tag name";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteLine("Ошибка: требуется указать точное название тега.");
            shell.WriteLine($"Использование: {Help}");
            return;
        }

        var tagName = args[0];
        var tagNameLower = tagName.ToLowerInvariant();

        var matchingTagIds = new List<ProtoId<TagPrototype>>();
        foreach (var tagPrototype in _prototypeManager.EnumeratePrototypes<TagPrototype>())
        {
            if (tagPrototype.ID.ToLowerInvariant() == tagNameLower)
            {
                matchingTagIds.Add(new ProtoId<TagPrototype>(tagPrototype.ID));
            }
        }

        if (matchingTagIds.Count == 0)
        {
            shell.WriteLine($"Ошибка: тег '{tagName}' не существует в прототипах.");
            return;
        }

        var tagSystem = _entityManager.EntitySysManager.GetEntitySystem<TagSystem>();

        // Собираем все сущности с указанным тегом (любым из совпадающих по регистру)
        var entitiesWithTag = new HashSet<EntityUid>();
        var query = _entityManager.EntityQueryEnumerator<TagComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            // Проверяем точное совпадение тега (полное название, не частичное) без учета регистра
            foreach (var tagId in matchingTagIds)
            {
                if (tagSystem.HasTag(uid, tagId))
                {
                    entitiesWithTag.Add(uid);
                    break; // Сущность уже добавлена, не нужно проверять другие теги
                }
            }
        }

        // Используем первый найденный тег для вывода
        var displayTagName = matchingTagIds[0].ToString();

        // Группируем сущности по их ProtoId
        var entityCounts = new Dictionary<string, int>();

        foreach (var entityUid in entitiesWithTag)
        {
            string entityKey;
            if (_entityManager.TryGetComponent<MetaDataComponent>(entityUid, out var metaData)
                && metaData.EntityPrototype != null)
            {
                // Используем ID прототипа для группировки
                entityKey = metaData.EntityPrototype.ID;
            }
            else
            {
                // Если прототип недоступен, используем UID как fallback
                entityKey = entityUid.ToString();
            }

            entityCounts.TryGetValue(entityKey, out var currentCount);
            entityCounts[entityKey] = currentCount + 1;
        }

        string message;
        var totalCount = entitiesWithTag.Count;

        if (totalCount == 0)
        {
            message = $"Не найдено сущностей с тегом '{displayTagName}'.";
        }
        else
        {
            var messageBuilder = new System.Text.StringBuilder();
            messageBuilder.AppendLine($"Всего сущностей с тегом '{displayTagName}': {totalCount}:");

            foreach (var (entityName, count) in entityCounts
                .OrderByDescending(kvp => kvp.Value)
                .ThenBy(kvp => kvp.Key))
            {
                messageBuilder.AppendLine($"{count} {entityName}");
            }

            message = messageBuilder.ToString().TrimEnd();
        }

        shell.WriteLine(message);

        _sawmill.Info($"{message.Replace(System.Environment.NewLine, " | ")}");
    }
}
