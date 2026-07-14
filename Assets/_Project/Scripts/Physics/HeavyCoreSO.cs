using UnityEngine;

namespace Chris.PachiRogue.Physics
{
    /// <summary>More mass and more damage, less float.</summary>
    [CreateAssetMenu(menuName = "PachiRogue/Mutators/Heavy Core", fileName = "Mutator_HeavyCore")]
    public sealed class HeavyCoreSO : PhysicsMutatorSO
    {
        [SerializeField] private float massMultiplier = 1.6f;
        [SerializeField] private float damageMultiplier = 1.5f;

        public override void Apply(PhysicsBodyContext ctx)
        {
            ctx.Body.mass *= massMultiplier;
            ctx.DamageScalar *= damageMultiplier;
        }

        public override void Remove(PhysicsBodyContext ctx)
        {
            ctx.Body.mass /= massMultiplier;
            ctx.DamageScalar /= damageMultiplier;
        }
    }
}
