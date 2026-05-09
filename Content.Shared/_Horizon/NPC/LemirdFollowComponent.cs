// Создай новый файл LemirdFollowComponent.cs:

using Robust.Shared.Serialization;

namespace Content.Shared._Horizon.NPC
{
    [RegisterComponent]
    public sealed partial class LemirdFollowComponent : Component
    {
        [DataField("target")]
        public EntityUid? Target;

        [DataField("followDistance")]
        public float FollowDistance = 2.0f;

        [DataField("maxFollowDistance")]
        public float MaxFollowDistance = 20.0f;

        [DataField("onlyFirstTarget")]
        public bool OnlyFirstTarget = true; // Следить только за первым увиденным

        [ViewVariables]
        public bool HasFoundFirstTarget = false; // Нашел ли уже первую цель
    }
}
