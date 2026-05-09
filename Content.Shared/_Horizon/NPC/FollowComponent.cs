using Robust.Shared.Serialization;
using Robust.Shared.Utility; // Добавить для SpriteSpecifier

namespace Content.Shared._Horizon.NPC
{
    [RegisterComponent]
    public sealed partial class FollowComponent : Component
    {
        [DataField("target")]
        public EntityUid? Target;

        [DataField("followDistance")]
        public float FollowDistance = 2.0f;

        [DataField("stopFollowingIfTooFar")]
        public bool StopFollowingIfTooFar = true;

        [DataField("maxFollowDistance")]
        public float MaxFollowDistance = 10.0f;
    }
}
