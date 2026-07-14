using UnityEngine;

namespace Chris.PachiRogue.Physics
{
    /// <summary>
    /// The single contact listener on the body. Reads marker components off
    /// whatever was hit and publishes typed events — content objects carry
    /// data-only IDs, never logic (CLAUDE.md).
    /// </summary>
    [RequireComponent(typeof(PhysicsBody))]
    public sealed class CollisionRouter : MonoBehaviour
    {
        private PhysicsBody _body;

        private void Awake()
        {
            _body = GetComponent<PhysicsBody>();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!_body.LaunchActive || _body.Context == null)
            {
                return;
            }

            GameObject other = collision.gameObject;

            var peg = other.GetComponent<Peg>();
            if (peg != null)
            {
                _body.Context.Bus.Publish(new PegHit
                {
                    Position = collision.GetContact(0).point,
                    Value = Mathf.RoundToInt(peg.value * _body.Context.DamageScalar)
                });
                return;
            }

            var bumper = other.GetComponent<Bumper>();
            if (bumper != null)
            {
                Vector2 away = (_body.Context.Body.position - (Vector2)other.transform.position).normalized;
                _body.Context.Body.AddForce(away * bumper.impulse, ForceMode2D.Impulse);
                _body.Context.Bus.Publish(new BumperHit
                {
                    Position = collision.GetContact(0).point,
                    Value = Mathf.RoundToInt(bumper.value * _body.Context.DamageScalar)
                });
                return;
            }

            var hazard = other.GetComponent<Hazard>();
            if (hazard != null)
            {
                _body.Context.Bus.Publish(new HazardHit { Damage = hazard.damage });
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_body.LaunchActive || _body.Context == null)
            {
                return;
            }

            var cup = other.GetComponent<Cup>();
            if (cup != null)
            {
                _body.Context.Bus.Publish(new CupEntered { Multiplier = cup.multiplier });
                _body.ResolveNow();
                return;
            }

            if (other.GetComponent<KillZone>() != null)
            {
                _body.ResolveNow();
            }
        }
    }
}
