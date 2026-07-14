using UnityEngine;

namespace Chris.PachiRogue.Physics
{
    // Content marker components: data only, no logic — CollisionRouter reads
    // them and publishes typed events (CLAUDE.md: no per-peg scripts).

    public sealed class Peg : MonoBehaviour
    {
        public int value = 10;
    }

    public sealed class Bumper : MonoBehaviour
    {
        public int value = 15;
        public float impulse = 6f;
    }

    public sealed class Cup : MonoBehaviour
    {
        public float multiplier = 2f;
    }

    public sealed class Hazard : MonoBehaviour
    {
        public int damage = 10;
    }

    /// <summary>Trigger below the cups; resolves a launch that missed everything.</summary>
    public sealed class KillZone : MonoBehaviour
    {
    }
}
