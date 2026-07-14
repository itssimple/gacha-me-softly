using UnityEngine;

namespace Chris.PachiRogue.Physics
{
    /// <summary>Restitution up: the body bounces livelier off everything.</summary>
    [CreateAssetMenu(menuName = "PachiRogue/Mutators/Bouncy Boots", fileName = "Mutator_BouncyBoots")]
    public sealed class BouncyBootsSO : PhysicsMutatorSO
    {
        [SerializeField] private float bouncinessBonus = 0.25f;

        public override void Apply(PhysicsBodyContext ctx)
        {
            ctx.MaterialInstance.bounciness = Mathf.Clamp01(ctx.MaterialInstance.bounciness + bouncinessBonus);
        }

        public override void Remove(PhysicsBodyContext ctx)
        {
            ctx.MaterialInstance.bounciness = Mathf.Clamp01(ctx.MaterialInstance.bounciness - bouncinessBonus);
        }
    }
}
