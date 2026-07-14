using UnityEngine;

namespace Chris.PachiRogue.Physics
{
    /// <summary>
    /// Base for composable physics mutators (GAME_DESIGN.md §3). Mutators are
    /// content assets applied to a <see cref="PhysicsBodyContext"/> at launch
    /// prep and removed cleanly afterwards; Apply/Remove must be symmetric so
    /// stacking works (verified by EditMode tests).
    /// </summary>
    public abstract class PhysicsMutatorSO : ScriptableObject
    {
        public abstract void Apply(PhysicsBodyContext ctx);

        public abstract void Remove(PhysicsBodyContext ctx);
    }
}
