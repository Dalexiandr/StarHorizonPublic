using System;
using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Content.Shared._Horizon.EventItems;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Player;

namespace Content.Server._Horizon.EventItems;

[UsedImplicitly]
public sealed class GrantEventItemEui : BaseEui
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;

    private readonly NetEntity _targetNetEntity;
    private readonly EntityUid _targetEntity;
    private ISawmill _sawmill;

    public GrantEventItemEui(NetEntity targetNetEntity, EntityUid targetEntity)
    {
        _targetNetEntity = targetNetEntity;
        _targetEntity = targetEntity;
        IoCManager.InjectDependencies(this);
        _sawmill = Logger.GetSawmill("event-items-eui");
    }

    public override void Opened()
    {
        base.Opened();
        _sawmill.Debug($"Grant Event Item EUI opened for entity {_targetEntity} by admin {Player.Name}.");
        StateDirty();
        _adminManager.OnPermsChanged += OnPermsChanged;
    }

    public override void Closed()
    {
        base.Closed();
        _sawmill.Debug($"Grant Event Item EUI closed by admin {Player.Name}.");
        _adminManager.OnPermsChanged -= OnPermsChanged;
    }

    public override EuiStateBase GetNewState()
    {
        var meta = _entManager.GetComponent<MetaDataComponent>(_targetEntity);
        var protoId = meta.EntityPrototype?.ID ?? string.Empty;

        var players = _playerManager.Sessions
            .Select(s => new EventItemPlayerInfo
            {
                UserId = s.UserId.UserId,
                UserName = s.Name,
                CharacterName = s.AttachedEntity != null
                    ? _entManager.GetComponent<MetaDataComponent>(s.AttachedEntity.Value).EntityName
                    : s.Name,
            })
            .ToList();

        return new GrantEventItemEuiState
        {
            TargetEntity = _targetNetEntity,
            EntityName = meta.EntityName,
            EntityDescription = meta.EntityDescription,
            PrototypeId = protoId,
            OnlinePlayers = players,
        };
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not GrantEventItemMessage grantMsg)
            return;

        var targetCkey = _playerManager.Sessions
            .FirstOrDefault(s => s.UserId.UserId == grantMsg.TargetPlayerUserId)?.Name ?? "offline";
        _sawmill.Info($"Admin {Player.Name} confirmed grant: entity {_targetEntity} -> player {targetCkey} ({grantMsg.TargetPlayerUserId}), cost: {grantMsg.CreditCost}.");

        // Validate admin permissions
        if (!_adminManager.HasAdminFlag(Player, AdminFlags.Debug))
        {
            _sawmill.Warning($"Player {Player.Name} tried to grant event item without Debug flag.");
            Close();
            return;
        }

        // Validate entity still exists
        if (!_entManager.EntityExists(_targetEntity))
        {
            _sawmill.Warning($"Target entity no longer exists.");
            Close();
            return;
        }

        var system = _entManager.System<EventItemsSystem>();
        system.GrantItemToPlayer(
            _targetEntity,
            grantMsg.TargetPlayerUserId,
            grantMsg.CreditCost,
            grantMsg.MaxUses,
            Player.Name);

        Close();
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs obj)
    {
        if (obj.Player == Player && !_adminManager.HasAdminFlag(Player, AdminFlags.Debug))
        {
            Close();
        }
    }
}
