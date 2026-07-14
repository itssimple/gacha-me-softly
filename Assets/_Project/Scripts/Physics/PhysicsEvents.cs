using UnityEngine;

namespace Chris.PachiRogue.Physics
{
    /// <summary>The body left the launcher; a resolution phase begins.</summary>
    public struct LaunchStarted
    {
    }

    /// <summary>
    /// The launch finished: the body came to rest, fell out of the stage, or
    /// was absorbed by a cup.
    /// </summary>
    public struct LaunchResolved
    {
    }

    /// <summary>A peg was struck. Value is already scaled by the body's damage scalar.</summary>
    public struct PegHit
    {
        public Vector2 Position;
        public int Value;
    }

    /// <summary>A bumper was struck (extra impulse, small score).</summary>
    public struct BumperHit
    {
        public Vector2 Position;
        public int Value;
    }

    /// <summary>The body entered a multiplier cup; the launch resolves.</summary>
    public struct CupEntered
    {
        public float Multiplier;
    }

    /// <summary>The body touched a hazard.</summary>
    public struct HazardHit
    {
        public int Damage;
    }
}
