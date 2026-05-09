using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.NPC
{
    /// <summary>
    /// Событие начала диалога
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class DialogueStartEvent : EntityEventArgs
    {
        public NetEntity Npc;
        public NetEntity User;

        public DialogueStartEvent(NetEntity npc, NetEntity user)
        {
            Npc = npc;
            User = user;
        }
    }

    /// <summary>
    /// Событие выбора ответа в диалоге
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class DialogueResponseEvent : EntityEventArgs
    {
        public NetEntity Npc;
        public NetEntity User;
        public DialogueResponse? Response;

        public DialogueResponseEvent(NetEntity npc, NetEntity user, DialogueResponse? response)
        {
            Npc = npc;
            User = user;
            Response = response;
        }
    }

    /// <summary>
    /// Сетевое событие для открытия UI диалога
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class OpenDialogueUiEvent : EntityEventArgs
    {
        public NetEntity Npc;
        public NetEntity User;

        public OpenDialogueUiEvent(NetEntity npc, NetEntity user)
        {
            Npc = npc;
            User = user;
        }
    }
}
