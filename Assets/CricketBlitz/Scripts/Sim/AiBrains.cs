using System;

namespace CricketBlitz.Sim
{
    public struct BowlerProfile
    {
        public string Name;
        public int Skill;
        public DeliveryType Stock;
        public bool LeftArm;
        public float BaseSpeedKmh;
    }

    public struct BatterProfile
    {
        public string Name;
        public int Technique;
        public int Timing;
        public BatsmanType Type;
        public bool RightHanded;
    }

    /// <summary>
    /// Bowling and batting choices for the side the player is not controlling.
    /// Higher difficulties bowl closer to a good length and time the reply better.
    /// </summary>
    public static class AiBrains
    {
        public static float Spray(Difficulty difficulty, float customSpray)
        {
            switch (difficulty)
            {
                case Difficulty.Easy:
                    return 0.62f;
                case Difficulty.Medium:
                    return 0.34f;
                case Difficulty.Hard:
                    return 0.22f;
                case Difficulty.Expert:
                    return 0.14f;
                case Difficulty.Hardcore:
                    return 0.08f;
                default:
                    return customSpray;
            }
        }

        public static float TimingScale(Difficulty difficulty, float customScale)
        {
            switch (difficulty)
            {
                case Difficulty.Easy:
                    return 1.45f;
                case Difficulty.Medium:
                    return 1.05f;
                case Difficulty.Hard:
                    return 0.86f;
                case Difficulty.Expert:
                    return 0.72f;
                case Difficulty.Hardcore:
                    return 0.58f;
                default:
                    return customScale;
            }
        }

        public static DeliveryPlan Bowl(
            BowlerProfile bowler,
            BatterProfile batter,
            Difficulty difficulty,
            PitchType pitch,
            Random random,
            float customSpray,
            bool powerplay)
        {
            float spray = Spray(difficulty, customSpray);
            spray *= 1.15f - Clamp01(bowler.Skill / 100f) * 0.45f;

            float line = (float)(random.NextDouble() * 2.0 - 1.0) * spray;
            // Attack the top of off, from the batter's hand.
            float offSide = batter.RightHanded ? 0.12f : -0.12f;
            line += offSide;

            float lengthNoise = spray * 3.2f;
            float bounceZ = 4.3f + (float)(random.NextDouble() * 2.0 - 1.0) * lengthNoise;

            if (difficulty == Difficulty.Easy && random.NextDouble() < 0.28)
            {
                bounceZ = 7.2f + (float)random.NextDouble() * 1.6f;
                line += (float)(random.NextDouble() * 0.4 - 0.2);
            }
            else if (difficulty == Difficulty.Hardcore && random.NextDouble() < 0.33)
            {
                bounceZ = random.NextDouble() < 0.5 ? 8.15f : 0.6f;
            }
            else if ((difficulty == Difficulty.Hard || difficulty == Difficulty.Expert) && random.NextDouble() < 0.22)
            {
                bounceZ = 7.8f;
            }

            if (pitch == PitchType.Dry && bowler.Stock == DeliveryType.LegCutter)
            {
                bounceZ = Math.Min(bounceZ, 5.2f);
            }

            if (bounceZ < -1.2f) bounceZ = -1.2f;
            if (bounceZ > 9.0f) bounceZ = 9.0f;

            DeliveryType type = bowler.Stock;
            double mix = random.NextDouble();
            if (mix < 0.18)
            {
                type = DeliveryType.Slower;
            }
            else if (mix < 0.34)
            {
                type = pitch == PitchType.Green ? DeliveryType.OutSwing : bowler.Stock;
            }
            else if (mix < 0.46 && (difficulty == Difficulty.Expert || difficulty == Difficulty.Hardcore))
            {
                type = DeliveryType.ReverseSwing;
            }

            float speed = bowler.BaseSpeedKmh;
            if (difficulty == Difficulty.Easy)
            {
                speed -= 12f;
            }
            else if (difficulty == Difficulty.Hard)
            {
                speed += 4f;
            }
            else if (difficulty == Difficulty.Expert || difficulty == Difficulty.Hardcore)
            {
                speed += 8f;
            }

            if (powerplay && difficulty >= Difficulty.Hard)
            {
                line *= 0.75f;
            }

            DeliveryPlan plan = DeliveryPlan.Make(type, speed, line, bounceZ, false);
            if (!batter.RightHanded)
            {
                // "Out-swing" was stored as +X. Away from a left-hander is -X.
                if (plan.Type == DeliveryType.OutSwing || plan.Type == DeliveryType.InSwing || plan.Type == DeliveryType.ReverseSwing)
                {
                    plan.Swing = -plan.Swing;
                }
            }

            if (bowler.LeftArm)
            {
                plan.RoundTheWicket = random.NextDouble() < 0.55;
            }

            plan.MarkedNoBall = random.NextDouble() < (difficulty == Difficulty.Easy ? 0.07 : 0.03);
            plan.BowlerName = bowler.Name;
            return plan;
        }

        public static ShotQuery Bat(
            BatterProfile batter,
            DeliveryPlan delivery,
            Difficulty difficulty,
            Random random,
            float customTimingScale)
        {
            ShotQuery query = new ShotQuery();
            query.Technique = batter.Technique;
            query.TimingSkill = batter.Timing;
            query.Type = batter.Type;
            query.StrikerRightHanded = batter.RightHanded;
            query.BounceZ = delivery.BounceZ;
            query.IncomingSpeedKmh = delivery.SpeedKmh;
            query.Power01 = 0.45f;
            query.DifficultyWindowScale = 1f;

            bool shortBall = delivery.BounceZ < 2.3f;
            bool full = delivery.BounceZ > 6.3f;
            bool wide = Math.Abs(delivery.LineX) > 0.48f;
            float offSign = batter.RightHanded ? 1f : -1f;

            if (shortBall)
            {
                query.Foot = Footwork.BackFoot;
                query.Family = batter.Type == BatsmanType.Defensive ? ShotFamily.Stroke : ShotFamily.Loft;
                query.AimAngleDeg = -80f * offSign;
                query.Power01 = 0.7f;
            }
            else if (full && Math.Abs(delivery.LineX) < 0.35f)
            {
                query.Foot = Footwork.FrontFoot;
                query.Family = batter.Type == BatsmanType.Brute ? ShotFamily.Loft : ShotFamily.Stroke;
                query.AimAngleDeg = 32f * offSign;
                query.Power01 = 0.8f;
            }
            else if (wide && batter.Type == BatsmanType.Defensive && difficulty >= Difficulty.Hard)
            {
                query.Foot = Footwork.Leave;
                query.Family = ShotFamily.Push;
                query.AimAngleDeg = 0f;
            }
            else if (delivery.BounceZ > 7.6f)
            {
                query.Foot = Footwork.Advance;
                query.Family = ShotFamily.Push;
                query.AimAngleDeg = 8f * offSign;
            }
            else
            {
                query.Foot = Footwork.FrontFoot;
                query.Family = ShotFamily.Stroke;
                query.AimAngleDeg = (delivery.LineX >= 0f ? 28f : -36f);
            }

            // The player-difficulty scale makes the AI less precise on Easy.
            float skillError = 0.14f - Clamp01(batter.Timing / 100f) * 0.09f;
            float diffError = 1f / Math.Max(0.45f, TimingScale(difficulty, customTimingScale));
            // Invert: easy has a large timing scale for the human, so the AI should miss more on easy.
            float humanHelp = TimingScale(difficulty, customTimingScale);
            float errorScale = skillError * (0.65f + humanHelp);
            if (difficulty == Difficulty.Hardcore)
            {
                errorScale *= 0.45f;
            }

            double roll = random.NextDouble() * 2.0 - 1.0;
            query.TimeError = (float)roll * errorScale * diffError;
            if (Math.Abs(delivery.LineX) > 0.7f)
            {
                query.TimeError *= 1.4f;
            }

            return query;
        }

        static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }
    }
}
