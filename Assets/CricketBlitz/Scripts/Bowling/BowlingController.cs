using CricketBlitz.Data;
using CricketBlitz.Sim;
using UnityEngine;

namespace CricketBlitz.Bowling
{
    public enum FieldPlan
    {
        Attacking1 = 0,
        Attacking2 = 1,
        Attacking3 = 2,
        Attacking4 = 3,
        Attacking5 = 4,
        Balanced = 5,
        Defensive = 6
    }

    /// <summary>
    /// The player's bowling setup: delivery, over or round, pitch marker and spin.
    /// The AI uses <see cref="AiBrains"/> instead of this setup.
    /// </summary>
    public sealed class BowlingController : MonoBehaviour
    {
        public DeliveryType Type = DeliveryType.Straight;
        public float LineX;
        public float BounceZ = 4.4f;
        public float Spin;
        public bool RoundTheWicket;
        public bool Ready;
        public FieldPlan Field = FieldPlan.Balanced;

        public void ResetForBall()
        {
            Ready = false;
            Spin = 0f;
        }

        public void Nudge(Vector2 joystick)
        {
            if (joystick.sqrMagnitude < 0.0004f)
            {
                return;
            }

            LineX = Mathf.Clamp(joystick.x * 0.8f, -0.9f, 0.9f);
            float along = joystick.y * 0.5f + 0.5f;
            BounceZ = Mathf.Lerp(-0.4f, 8.5f, along);
        }

        public void SetType(DeliveryType type)
        {
            Type = type;
        }

        public void ToggleSide()
        {
            RoundTheWicket = !RoundTheWicket;
        }

        public void AddSpin(float delta)
        {
            Spin = Mathf.Clamp(Spin + delta, -1f, 1f);
        }

        public void CycleField(int direction)
        {
            int count = 7;
            int next = ((int)Field + direction) % count;
            if (next < 0)
            {
                next += count;
            }

            Field = (FieldPlan)next;
        }

        public void MarkReady()
        {
            Ready = true;
        }

        public DeliveryPlan BuildPlan(PlayerSeed bowler, bool strikerRightHanded)
        {
            float speed = bowler != null ? bowler.Speed : 128f;
            DeliveryPlan plan = DeliveryPlan.Make(Type, speed, LineX, BounceZ, RoundTheWicket);
            if (Type == DeliveryType.LegCutter || Type == DeliveryType.OffCutter || Type == DeliveryType.Straight || Type == DeliveryType.Slower)
            {
                plan.Spin += Spin;
                plan.Spin = Mathf.Clamp(plan.Spin, -1f, 1f);
            }
            else
            {
                plan.Swing += Spin * 0.35f;
            }

            if (!strikerRightHanded && (Type == DeliveryType.OutSwing || Type == DeliveryType.InSwing || Type == DeliveryType.ReverseSwing))
            {
                plan.Swing = -plan.Swing;
            }

            plan.MarkedNoBall = false;
            plan.BowlerName = bowler != null ? bowler.Name : "Bowler";
            return plan;
        }

        public static string FieldLabel(FieldPlan plan)
        {
            switch (plan)
            {
                case FieldPlan.Attacking1: return "Attacking 1";
                case FieldPlan.Attacking2: return "Attacking 2";
                case FieldPlan.Attacking3: return "Attacking 3";
                case FieldPlan.Attacking4: return "Attacking 4";
                case FieldPlan.Attacking5: return "Attacking 5";
                case FieldPlan.Defensive: return "Defensive";
                default: return "Balanced";
            }
        }

        public static float AttackWeight(FieldPlan plan)
        {
            switch (plan)
            {
                case FieldPlan.Attacking1: return 0.58f;
                case FieldPlan.Attacking2: return 0.7f;
                case FieldPlan.Attacking3: return 0.82f;
                case FieldPlan.Attacking4: return 0.92f;
                case FieldPlan.Attacking5: return 1f;
                case FieldPlan.Defensive: return 0.05f;
                default: return 0.38f;
            }
        }
    }
}
