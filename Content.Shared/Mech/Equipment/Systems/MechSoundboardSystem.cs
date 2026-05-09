using System.Linq;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Mech.Equipment.Systems;

/// <summary>
/// Handles everything for mech soundboard.
/// </summary>
public sealed class MechSoundboardSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechSoundboardComponent, MechEquipmentUiStateReadyEvent>(OnUiStateReady);
        SubscribeLocalEvent<MechSoundboardComponent, MechEquipmentUiMessageRelayEvent<MechSoundboardPlayMessage>>(OnSoundboardMessage);
    }

    private void OnUiStateReady(EntityUid uid, MechSoundboardComponent comp, MechEquipmentUiStateReadyEvent args)
    {
        // you have to specify a collection so it must exist probably
        var sounds = comp.Sounds.Select(sound => sound.Collection!);

        // Horizon - изменение передачи состояний интерфейса
        /*var state = new MechSoundboardUiState
        {
            Sounds = sounds.ToList()
        };
        args.States.Add(GetNetEntity(uid), state);*/

        args.State = new MechSoundboardUiState
        {
            Sounds = sounds.ToList()
        };
    }

    private void OnSoundboardMessage(EntityUid uid, MechSoundboardComponent comp, MechEquipmentUiMessageRelayEvent<MechSoundboardPlayMessage> args)
    {
        if (!TryComp<MechEquipmentComponent>(uid, out var equipment) ||
            equipment.EquipmentOwner == null)
            return;

        if (args.Message.Sound >= comp.Sounds.Count)
            return;

        if (TryComp(uid, out UseDelayComponent? useDelay)
            && !_useDelay.TryResetDelay((uid, useDelay), true))
            return;

        // honk!!!!!
        _audio.PlayPredicted(comp.Sounds[args.Message.Sound], uid, GetEntity(args.Pilot));   // Horizon Mech
    }
}
