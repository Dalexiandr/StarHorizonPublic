using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using System.Collections.Generic;

namespace Content.Server.Mining.Components
{
    /// <summary>
    /// Параметры жилы для бура
    /// </summary>
    [RegisterComponent]
    [Virtual]
    public sealed partial class OreDepositComponent : Component
    {
        /// <summary>
        /// Количества руды и тип
        /// </summary>
        [DataField("oreCounts")]
        public Dictionary<string, int> OreCounts { get; private set; } = new();

        /// <summary>
        /// Твёрдость жилы, чем больше тем медленее добыча
        /// </summary>
        [DataField("hardness")]
        public float Hardness { get; private set; } = 1.0f;
    }
}
