using CricketBlitz.Sim;
using UnityEngine;

namespace CricketBlitz.Batting
{
    /// <summary>
    /// Reads the shot the player has prepared. The joystick sets direction,
    /// the buttons set Push / Stroke / Loft and the footwork, and releasing
    /// the shot button is the timing input.
    /// </summary>
    public sealed class BattingController : MonoBehaviour
    {
        public Footwork Foot = Footwork.FrontFoot;
        public ShotFamily Planned = ShotFamily.Push;
        public float Power01;
        public Vector2 Aim;
        public bool WindowOpen;
        public bool Committed;
        public bool ForceLeave;

        bool holdingLoft;
        ShotFamily heldFamily;

        public void ResetForBall()
        {
            Power01 = 0f;
            holdingLoft = false;
            Committed = false;
            ForceLeave = false;
            Aim = Vector2.zero;
        }

        public void SetAim(Vector2 aim)
        {
            Aim = Vector2.ClampMagnitude(aim, 1f);
        }

        public void SetFoot(Footwork foot)
        {
            Foot = foot;
            if (foot != Footwork.Leave)
            {
                ForceLeave = false;
            }
        }

        public void PlayLeave()
        {
            Foot = Footwork.Leave;
            ForceLeave = true;
            if (WindowOpen)
            {
                Commit(ShotFamily.Push);
            }
        }

        public void CycleFoot()
        {
            int next = ((int)Foot + 1) % 4;
            Foot = (Footwork)next;
        }

        public void Hold(ShotFamily family, bool down)
        {
            if (!WindowOpen)
            {
                if (down)
                {
                    Planned = family;
                }

                return;
            }

            if (down)
            {
                if (family != ShotFamily.Push)
                {
                    ForceLeave = false;
                    if (Foot == Footwork.Leave)
                    {
                        Foot = Footwork.FrontFoot;
                    }
                }

                holdingLoft = family == ShotFamily.Loft;
                heldFamily = family;
                Planned = family;
                if (!holdingLoft)
                {
                    Power01 = family == ShotFamily.Push ? 0.15f : 0.45f;
                }
            }
            else if (heldFamily == family || family == Planned)
            {
                Commit(family);
            }
        }

        public void Commit(ShotFamily family)
        {
            if (!WindowOpen || Committed)
            {
                return;
            }

            Planned = family;
            Committed = true;
            holdingLoft = false;
        }

        public void Tick(float dt)
        {
            if (WindowOpen && holdingLoft)
            {
                Power01 = Mathf.MoveTowards(Power01, 1f, dt / 0.55f);
            }
        }

        public bool TryConsume(BatterScore batter, DeliveryPlan plan, float timeToContact, float windowScale, out ShotQuery query)
        {
            query = default(ShotQuery);
            if (!Committed || batter == null)
            {
                return false;
            }

            Committed = false;
            query = Build(batter, plan, timeToContact, windowScale, Foot == Footwork.Leave);
            return true;
        }

        public ShotQuery LeaveQuery(BatterScore batter, DeliveryPlan plan, float windowScale)
        {
            return Build(batter, plan, 0f, windowScale, true);
        }

        ShotQuery Build(BatterScore batter, DeliveryPlan plan, float timeToContact, float windowScale, bool leave)
        {
            ShotQuery query = new ShotQuery();
            query.Technique = batter.Technique;
            query.TimingSkill = batter.Timing;
            query.Type = batter.Type;
            query.StrikerRightHanded = batter.RightHanded;
            query.BounceZ = plan.BounceZ;
            query.IncomingSpeedKmh = plan.SpeedKmh;
            query.DifficultyWindowScale = windowScale;
            query.Family = Planned;
            query.Foot = leave || ForceLeave ? Footwork.Leave : Foot;
            query.Power01 = Power01;
            query.TimeError = leave ? 1f : -timeToContact;
            query.AimAngleDeg = AimAngle(batter.RightHanded);
            return query;
        }

        float AimAngle(bool rightHanded)
        {
            if (Aim.sqrMagnitude < 0.04f)
            {
                return 0f;
            }

            float angle = Mathf.Atan2(Aim.x, Aim.y) * Mathf.Rad2Deg;
            if (!rightHanded)
            {
                angle = -angle;
            }

            return Mathf.Clamp(angle, -170f, 170f);
        }
    }
}
