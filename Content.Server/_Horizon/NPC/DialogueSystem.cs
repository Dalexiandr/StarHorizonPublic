using Content.Shared._Horizon.NPC;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Robust.Shared.Prototypes;
using Content.Server.Chat.Systems;
using Content.Shared.Mobs.Systems;

namespace Content.Server._Horizon.NPC
{
    public sealed class DialogueSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DialogueComponent, GetVerbsEvent<InteractionVerb>>(AddDialogueVerbs);
        }

        private void AddDialogueVerbs(EntityUid uid, DialogueComponent component, GetVerbsEvent<InteractionVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            if (!TryComp<DialogueStateComponent>(uid, out var state))
                return;

            switch (state.State)
            {
                case DialogueState.Idle:
                    var talkVerb = new InteractionVerb
                    {
                        Act = () => StartDialogue(uid, args.User, component),
                        Text = Loc.GetString("verb-dialogue-talk"),
                        Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/sentient.svg.192dpi.png")),
                        Priority = 2
                    };
                    args.Verbs.Add(talkVerb);
                    break;

                case DialogueState.Talking when state.CurrentResponse == "follow":
                    AddResponseVerbs(uid, args.User, args);
                    break;

                case DialogueState.Following:
                    var stopFollowVerb = new InteractionVerb
                    {
                        Act = () => StopFollow(uid, args.User),
                        Text = Loc.GetString("verb-dialogue-stop-follow"),
                        Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/close.svg.192dpi.png")),
                        Priority = 3
                    };
                    args.Verbs.Add(stopFollowVerb);
                    break;
            }
        }

        private void AddResponseVerbs(EntityUid npc, EntityUid user, GetVerbsEvent<InteractionVerb> args)
        {
            var replyCategory = new VerbCategory(Loc.GetString("verb-dialogue-reply-category"), "/Textures/Interface/VerbIcons/information.svg.192dpi.png");

            var acceptVerb = new InteractionVerb
            {
                Act = () => AcceptFollow(npc, user),
                Text = Loc.GetString("verb-dialogue-accept"),
                Category = replyCategory,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/plus.svg.192dpi.png")),
                Priority = 1
            };
            args.Verbs.Add(acceptVerb);

            var declineVerb = new InteractionVerb
            {
                Act = () => DeclineFollow(npc, user),
                Text = Loc.GetString("verb-dialogue-decline"),
                Category = replyCategory,
                Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/smite.svg.192dpi.png")),
                Priority = 2
            };
            args.Verbs.Add(declineVerb);
        }

        public void StartDialogue(EntityUid npc, EntityUid user, DialogueComponent? component = null)
        {
            if (!Resolve(npc, ref component))
                return;

            if (_mobState.IsDead(npc) || EntityManager.IsQueuedForDeletion(npc))
                return;

            var state = EnsureComp<DialogueStateComponent>(npc);
            state.State = DialogueState.Talking;

            if (!_prototypeManager.TryIndex<DialogueTreePrototype>(component.DialogueTree, out var dialogueTree))
                return;

            DialogueEntry? dialogue = null;
            foreach (var entry in dialogueTree.Dialogues)
            {
                if (entry.Id == "start")
                {
                    dialogue = entry;
                    break;
                }
            }

            if (dialogue != null && dialogue.Responses.Count > 0)
            {
                state.CurrentResponse = dialogue.Responses[0].Action;

                _chatSystem.TrySendInGameICMessage(
                    npc,
                    dialogue.Text,
                    InGameICChatType.Speak,
                    false
                );
            }
        }

        private void AcceptFollow(EntityUid npc, EntityUid user)
        {
            if (_mobState.IsDead(npc) || EntityManager.IsQueuedForDeletion(npc))
                return;

            if (!TryComp<DialogueStateComponent>(npc, out var state))
                return;

            state.State = DialogueState.Following;
            state.CurrentResponse = null;

            _chatSystem.TrySendInGameICMessage(
                npc,
                Loc.GetString("dialogue-npc-accept-follow"),
                InGameICChatType.Speak,
                false
            );

            var follow = EnsureComp<FollowComponent>(npc);
            follow.Target = user;
        }

        private void DeclineFollow(EntityUid npc, EntityUid user)
        {
            if (!TryComp<DialogueStateComponent>(npc, out var state))
                return;

            state.State = DialogueState.Idle;
            state.CurrentResponse = null;

            _chatSystem.TrySendInGameICMessage(
                npc,
                Loc.GetString("dialogue-npc-decline-follow"),
                InGameICChatType.Speak,
                false
            );
        }

        private void StopFollow(EntityUid npc, EntityUid user)
        {
            if (!TryComp<DialogueStateComponent>(npc, out var state))
                return;

            state.State = DialogueState.Idle;

            RemComp<FollowComponent>(npc);

            _chatSystem.TrySendInGameICMessage(
                npc,
                Loc.GetString("dialogue-npc-stop-follow"),
                InGameICChatType.Speak,
                false
            );
        }

        private void AddStateVerbs(EntityUid uid, DialogueStateComponent component, GetVerbsEvent<InteractionVerb> args)
        {
            // Этот метод будет вызван автоматически для всех сущностей с DialogueStateComponent
            // Здесь можно добавить дополнительную логику если нужно
        }
    }
}
