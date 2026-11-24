using System;

namespace CricketBlitz.Sim
{
    public struct BallState
    {
        public Vec3 Pos;
        public Vec3 Vel;
        public bool HasBounced;
        public bool HasBeenHit;
        public bool BouncedSinceHit;
        public bool Alive;
        public bool Resting;
        public bool IsThrow;
        public int BounceCount;
    }

    /// <summary>
    /// Kinematic cricket ball. Gravity, pitch bounce, pre-bounce swing and post-bounce cut.
    /// The pitch lies on Z, stumps at z = +/- StumpZ, batter facing the bowler from +Z.
    /// </summary>
    public static class BallSim
    {
        public const float Gravity = 9.81f;
        public const float BallRadius = 0.036f;
        public const float BoundaryRadius = 56f;
        public const float StumpZ = 10.06f;
        public const float StumpHalfWidth = 0.16f;
        public const float StumpHeight = 0.71f;
        public const float ContactZ = 9.15f;
        public const float ReleaseZ = -12.4f;
        public const float WideAbsX = 0.62f;
        public const float NoBallHeight = 1.35f;

        public static float Restitution(PitchType pitch)
        {
            switch (pitch)
            {
                case PitchType.Hard:
                    return 0.62f;
                case PitchType.Dry:
                    return 0.40f;
                default:
                    return 0.50f;
            }
        }

        public static float SwingScale(PitchType pitch)
        {
            switch (pitch)
            {
                case PitchType.Green:
                    return 1.30f;
                case PitchType.Hard:
                    return 0.80f;
                default:
                    return 0.62f;
            }
        }

        public static float SpinKick(PitchType pitch)
        {
            switch (pitch)
            {
                case PitchType.Dry:
                    return 1.45f;
                case PitchType.Green:
                    return 0.85f;
                default:
                    return 0.40f;
            }
        }

        public static BallState Release(in DeliveryPlan plan)
        {
            float side = plan.RoundTheWicket ? -0.7f : 0.55f;
            BallState state = new BallState();
            state.Pos = new Vec3(side, 2.05f, ReleaseZ);
            state.Alive = true;

            Vec3 bounce = new Vec3(plan.LineX, BallRadius, plan.BounceZ);
            float speed = plan.SpeedMs;
            float dist = Vec3.DistanceXZ(state.Pos, bounce);
            float t = dist / Math.Max(8f, speed * 0.97f);
            Vec3 flat = new Vec3(bounce.X - state.Pos.X, 0f, bounce.Z - state.Pos.Z);
            Vec3 vel = Vec3.NormalizeXZ(flat) * (dist / t);
            vel.Y = (bounce.Y - state.Pos.Y + 0.5f * Gravity * t * t) / t;
            state.Vel = vel;
            return state;
        }

        /// <summary>
        /// Advances the ball by dt. Returns the position from the start of the step
        /// so callers can test the segment against stumps and the boundary.
        /// </summary>
        public static Vec3 Step(ref BallState state, float dt, PitchType pitch, float swing, float spin, float wind)
        {
            Vec3 previous = state.Pos;
            if (!state.Alive || state.Resting)
            {
                return previous;
            }

            if (dt < 0.0001f)
            {
                return previous;
            }

            Vec3 vel = state.Vel;
            vel.Y -= Gravity * dt;

            if (!state.HasBounced && !state.HasBeenHit && !state.IsThrow)
            {
                float swingAccel = swing * 3.1f * SwingScale(pitch);
                vel.X += swingAccel * dt;
            }

            vel.X += wind * dt;

            Vec3 pos = state.Pos + vel * dt;

            if (pos.Y <= BallRadius && vel.Y < 0f)
            {
                pos.Y = BallRadius;
                bool onPitch = Math.Abs(pos.X) < 1.55f && Math.Abs(pos.Z) < 10.3f;
                float e = Restitution(pitch);
                if (!onPitch)
                {
                    e *= 0.45f;
                }

                vel.Y = -vel.Y * e;
                float grip = onPitch ? 0.9f : 0.68f;
                vel.X *= grip;
                vel.Z *= grip;

                if (!state.HasBeenHit && !state.IsThrow && onPitch)
                {
                    vel.X += spin * SpinKick(pitch);
                }

                state.HasBounced = true;
                state.BounceCount += 1;
                if (state.HasBeenHit)
                {
                    state.BouncedSinceHit = true;
                }

                float horizontal = (float)Math.Sqrt(vel.X * vel.X + vel.Z * vel.Z);
                if (vel.Y < 0.45f && horizontal < 1.6f)
                {
                    vel.Y = 0f;
                    vel.X *= 0.8f;
                    vel.Z *= 0.8f;
                }
            }

            state.Pos = pos;
            state.Vel = vel;

            float speed = vel.Magnitude;
            if (state.HasBounced && speed < 0.45f && pos.Y <= BallRadius + 0.02f)
            {
                state.Resting = true;
                state.Vel = new Vec3(0f, 0f, 0f);
            }

            return previous;
        }

        public static void ApplyHit(ref BallState state, Vec3 velocity)
        {
            state.HasBeenHit = true;
            state.BouncedSinceHit = false;
            state.HasBounced = false;
            state.Resting = false;
            state.Alive = true;
            state.IsThrow = false;
            state.Vel = velocity;
            if (state.Pos.Y < 0.4f)
            {
                state.Pos.Y = 0.7f;
            }
        }

        public static bool SegmentHitsStumps(Vec3 a, Vec3 b, float stumpZ)
        {
            float height;
            return SegmentHitsStumps(a, b, stumpZ, out height);
        }

        public static bool SegmentHitsStumps(Vec3 a, Vec3 b, float stumpZ, out float height)
        {
            height = 0f;
            float dz = b.Z - a.Z;
            if (Math.Abs(dz) < 0.00001f)
            {
                return false;
            }

            float t = (stumpZ - a.Z) / dz;
            if (t < 0f || t > 1f)
            {
                return false;
            }

            float x = a.X + (b.X - a.X) * t;
            float y = a.Y + (b.Y - a.Y) * t;
            height = y;
            return Math.Abs(x) <= StumpHalfWidth && y >= 0f && y <= StumpHeight;
        }

        public static bool CrossedPlane(Vec3 a, Vec3 b, float planeZ, out Vec3 at)
        {
            at = b;
            float dz = b.Z - a.Z;
            if (Math.Abs(dz) < 0.00001f)
            {
                return false;
            }

            // The batter stands on +Z and the ball arrives from -Z, so a legal
            // crossing moves from below the plane to past it.
            if (a.Z >= planeZ || b.Z < planeZ)
            {
                return false;
            }

            float t = (planeZ - a.Z) / dz;
            at = new Vec3(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                planeZ);
            return true;
        }

        /// <summary>
        /// 4 if the ball crossed the rope along the ground, 6 if it crossed in the air
        /// before bouncing after the hit, otherwise 0.
        /// </summary>
        public static int BoundaryRuns(Vec3 previous, Vec3 next, bool hasBeenHit, bool bouncedSinceHit)
        {
            if (!hasBeenHit)
            {
                return 0;
            }

            float r0 = previous.MagnitudeXZ;
            float r1 = next.MagnitudeXZ;
            if (r0 <= BoundaryRadius && r1 > BoundaryRadius)
            {
                float t = (BoundaryRadius - r0) / Math.Max(0.0001f, r1 - r0);
                float y = previous.Y + (next.Y - previous.Y) * Clamp01(t);
                if (!bouncedSinceHit || y > 0.8f)
                {
                    return 6;
                }

                return 4;
            }

            return 0;
        }

        public static bool IsWide(Vec3 atContact, bool hasBeenHit)
        {
            if (hasBeenHit)
            {
                return false;
            }

            return Math.Abs(atContact.X) >= WideAbsX && atContact.Y < 1.8f;
        }

        public static bool IsAboveWaistNoBall(Vec3 atContact, bool hadBounced)
        {
            return !hadBounced && atContact.Y >= NoBallHeight;
        }

        static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }
    }
}
