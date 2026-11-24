using CricketBlitz.Sim;
using CricketBlitz.World;
using UnityEngine;

namespace CricketBlitz.Match
{
    /// <summary>
    /// Scene object for the ball. All flight, swing, bounce and stump tests
    /// go through <see cref="BallSim"/> so the same numbers are covered by the sim tests.
    /// </summary>
    public sealed class BallPhysics : MonoBehaviour
    {
        public BallState State;
        public DeliveryPlan Plan;
        public PitchType Pitch = PitchType.Hard;
        public float Wind;
        public float ReleaseSpeedKmh;
        public bool Tracking = true;

        TrailRenderer trail;
        float accumulator;

        public float SpeedKmh
        {
            get { return State.Vel.Magnitude * 3.6f; }
        }

        public static BallPhysics Create(Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Ball";
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * 0.18f;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GameMaterials.Get(new Color(0.72f, 0.12f, 0.1f));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            BallPhysics ball = go.AddComponent<BallPhysics>();
            ball.BuildTrail();
            go.SetActive(false);
            return ball;
        }

        void BuildTrail()
        {
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.22f;
            trail.startWidth = 0.07f;
            trail.endWidth = 0.01f;
            trail.material = GameMaterials.Get(new Color(1f, 0.85f, 0.75f));
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.enabled = false;
        }

        public void Release(DeliveryPlan plan, PitchType pitch, float wind)
        {
            Plan = plan;
            Pitch = pitch;
            Wind = wind;
            ReleaseSpeedKmh = plan.SpeedKmh;
            State = BallSim.Release(plan);
            accumulator = 0f;
            gameObject.SetActive(true);
            trail.enabled = true;
            trail.Clear();
            ApplyTransform();
        }

        public void Hit(Vec3 velocity)
        {
            BallSim.ApplyHit(ref State, velocity);
            ApplyTransform();
        }

        public void ThrowTo(Vector3 target, float speed)
        {
            Vector3 from = transform.position;
            Vector3 to = target - from;
            float distance = to.magnitude;
            float time = Mathf.Max(0.2f, distance / Mathf.Max(12f, speed));
            Vector3 velocity = to / time;
            velocity.y = (target.y - from.y + 0.5f * BallSim.Gravity * time * time) / time;
            State.Vel = new Vec3(velocity.x, velocity.y, velocity.z);
            State.Alive = true;
            State.Resting = false;
            State.IsThrow = true;
            State.HasBeenHit = true;
            State.BouncedSinceHit = true;
        }

        public void StopDead()
        {
            State.Alive = false;
            State.Resting = true;
            State.Vel = new Vec3(0f, 0f, 0f);
        }

        /// <summary>
        /// Steps the ball in small slices and reports each slice so stump and
        /// boundary checks cannot skip a crossing on a long frame.
        /// </summary>
        public void Tick(float dt, System.Action<Vec3, Vec3> onSegment)
        {
            if (!State.Alive)
            {
                return;
            }

            accumulator += dt;
            if (accumulator > 0.05f)
            {
                accumulator = 0.05f;
            }

            float swing = State.HasBeenHit || State.IsThrow ? 0f : Plan.Swing;
            float spin = State.HasBeenHit || State.IsThrow ? 0f : Plan.Spin;
            while (accumulator >= 0.008f && State.Alive && !State.Resting)
            {
                Vec3 before = BallSim.Step(ref State, 0.008f, Pitch, swing, spin, Wind);
                if (onSegment != null)
                {
                    onSegment(before, State.Pos);
                }

                accumulator -= 0.008f;
            }

            if (Tracking)
            {
                ApplyTransform();
            }
        }

        public float TimeToContact()
        {
            float dz = BallSim.ContactZ - State.Pos.Z;
            if (State.Vel.Z < 0.5f)
            {
                return dz > 0f ? 2f : -0.2f;
            }

            return dz / State.Vel.Z;
        }

        public void Snap(Vector3 world)
        {
            transform.position = world;
            State.Pos = new Vec3(world.x, world.y, world.z);
        }

        void ApplyTransform()
        {
            transform.position = new Vector3(State.Pos.X, State.Pos.Y, State.Pos.Z);
        }
    }
}
