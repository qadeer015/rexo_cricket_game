using System;

namespace CricketBlitz.Sim
{
    public struct ShotQuery
    {
        public float TimeError;
        public int Technique;
        public int TimingSkill;
        public BatsmanType Type;
        public ShotFamily Family;
        public Footwork Foot;
        public float AimAngleDeg;
        public float Power01;
        public float BounceZ;
        public float IncomingSpeedKmh;
        public float DifficultyWindowScale;
        public bool StrikerRightHanded;
    }

    public struct ShotResult
    {
        public TimingGrade Grade;
        public string ShotName;
        public bool MadeContact;
        public bool Edge;
        public float Quality;
        public Vec3 Velocity;
        public float ElevationDeg;
    }

    /// <summary>
    /// Turns a timed shot into a launch velocity. Perfect contact is a window around
    /// the ball arriving at the batter, widened by the Timing stat and narrowed by difficulty.
    /// </summary>
    public static class ShotResolver
    {
        public static float PerfectWindow(int timingSkill, float difficultyScale)
        {
            float skill = Clamp01(timingSkill / 100f);
            float window = 0.055f + skill * 0.07f;
            if (difficultyScale < 0.45f)
            {
                difficultyScale = 0.45f;
            }

            return window * difficultyScale;
        }

        public static TimingGrade GradeOf(float timeError, float perfectWindow)
        {
            float abs = Math.Abs(timeError);
            if (abs <= perfectWindow)
            {
                return TimingGrade.Perfect;
            }

            if (timeError < 0f && abs <= perfectWindow * 2.6f)
            {
                return TimingGrade.Early;
            }

            if (timeError > 0f && abs <= perfectWindow * 2.4f)
            {
                return TimingGrade.Late;
            }

            return TimingGrade.Miss;
        }

        public static ShotResult Resolve(in ShotQuery query)
        {
            ShotResult result = new ShotResult();
            result.ShotName = NameShot(query.AimAngleDeg, query.Family, query.Foot, query.BounceZ);

            if (query.Foot == Footwork.Leave)
            {
                result.Grade = TimingGrade.Miss;
                result.ShotName = "Leave";
                result.MadeContact = false;
                return result;
            }

            float window = PerfectWindow(query.TimingSkill, query.DifficultyWindowScale);
            float error = query.TimeError;
            bool shortBall = query.BounceZ < 2.4f;
            bool fullBall = query.BounceZ > 6.4f;

            if (shortBall && query.Foot == Footwork.BackFoot)
            {
                window *= 1.12f;
            }
            else if (fullBall && (query.Foot == Footwork.FrontFoot || query.Foot == Footwork.Advance))
            {
                window *= 1.1f;
            }

            if (shortBall && (query.Foot == Footwork.FrontFoot || query.Foot == Footwork.Advance))
            {
                error *= 1.35f;
            }

            if (fullBall && query.Foot == Footwork.BackFoot)
            {
                error *= 1.35f;
            }

            switch (query.Type)
            {
                case BatsmanType.Defensive:
                    window *= 1.12f;
                    break;
                case BatsmanType.Brute:
                    window *= 0.9f;
                    break;
                case BatsmanType.Radical:
                    window *= 0.92f;
                    break;
            }

            TimingGrade grade = GradeOf(error, window);
            result.Grade = grade;
            if (grade == TimingGrade.Miss)
            {
                result.MadeContact = false;
                result.ShotName = "Beaten";
                return result;
            }

            float technique = Clamp01(query.Technique / 100f);
            float speed = BaseSpeed(query.Family, query.Power01);
            speed *= 0.84f + technique * 0.32f;
            switch (grade)
            {
                case TimingGrade.Perfect:
                    speed *= 1f;
                    break;
                case TimingGrade.Early:
                    speed *= 0.74f;
                    break;
                default:
                    speed *= 0.7f;
                    break;
            }

            switch (query.Type)
            {
                case BatsmanType.Brute:
                    if (query.Family == ShotFamily.Loft)
                    {
                        speed *= 1.14f;
                    }

                    break;
                case BatsmanType.Defensive:
                    if (query.Family == ShotFamily.Loft)
                    {
                        speed *= 0.78f;
                    }
                    else if (query.Family == ShotFamily.Push)
                    {
                        speed *= 1.08f;
                    }

                    break;
                case BatsmanType.Radical:
                    if (query.Family != ShotFamily.Push)
                    {
                        speed *= 1.06f;
                    }

                    break;
            }

            // A fraction of the bowler's pace carries onto a grounded stroke.
            if (query.Family != ShotFamily.Loft)
            {
                speed += query.IncomingSpeedKmh / 3.6f * 0.06f;
            }

            float elevation = Elevation(query.Family, query.Power01, grade);
            float aim = query.AimAngleDeg;
            if (grade == TimingGrade.Early)
            {
                aim -= 14f;
            }
            else if (grade == TimingGrade.Late)
            {
                aim += 12f;
            }

            bool edge = grade != TimingGrade.Perfect && Math.Abs(query.TimeError) > window * 1.7f;
            if (edge)
            {
                result.Edge = true;
                speed *= 0.55f;
                elevation = Math.Max(8f, elevation * 0.5f);
                aim = query.StrikerRightHanded ? 150f : -150f;
                result.ShotName = "Outside Edge";
            }

            result.MadeContact = true;
            result.Quality = speed / 40f;
            result.ElevationDeg = elevation;
            result.Velocity = Launch(aim, elevation, speed);
            return result;
        }

        public static string NameShot(float angle, ShotFamily family, Footwork foot, float bounceZ)
        {
            if (foot == Footwork.Leave)
            {
                return "Leave";
            }

            bool loft = family == ShotFamily.Loft;
            bool back = foot == Footwork.BackFoot;
            if (angle <= -50f && angle >= -120f && (back || bounceZ < 3.2f || family == ShotFamily.Push))
            {
                if (family == ShotFamily.Push)
                {
                    return "Sweep";
                }

                return loft ? "Hook" : "Pull";
            }

            if (angle < -120f || angle > 155f)
            {
                return "Glance";
            }

            if (Math.Abs(angle) <= 18f)
            {
                if (family == ShotFamily.Push)
                {
                    return "Forward Defence";
                }

                return loft ? "Lofted Drive" : "Straight Drive";
            }

            if (angle > 18f && angle <= 55f)
            {
                return loft ? "Lofted Cover Drive" : "Cover Drive";
            }

            if (angle > 55f && angle <= 115f)
            {
                return back ? "Cut" : "Square Drive";
            }

            if (angle > 115f)
            {
                return "Late Cut";
            }

            if (angle < -18f && angle >= -55f)
            {
                return loft ? "Slog Sweep" : "On Drive";
            }

            return loft ? "Lofted Shot" : "Stroke";
        }

        static float BaseSpeed(ShotFamily family, float power)
        {
            switch (family)
            {
                case ShotFamily.Push:
                    return 15f;
                case ShotFamily.Loft:
                    return 20f + Clamp01(power) * 16f;
                default:
                    return 26f;
            }
        }

        static float Elevation(ShotFamily family, float power, TimingGrade grade)
        {
            float earlyPop = grade == TimingGrade.Early ? 8f : 0f;
            switch (family)
            {
                case ShotFamily.Push:
                    return 3f + earlyPop;
                case ShotFamily.Loft:
                    return 26f + Clamp01(power) * 22f;
                default:
                    return 7f + earlyPop;
            }
        }

        public static Vec3 Launch(float aimAngleDeg, float elevationDeg, float speed)
        {
            float aim = aimAngleDeg * ((float)Math.PI / 180f);
            float elev = elevationDeg * ((float)Math.PI / 180f);
            float horizontal = speed * (float)Math.Cos(elev);
            float x = (float)Math.Sin(aim) * horizontal;
            float z = -(float)Math.Cos(aim) * horizontal;
            float y = speed * (float)Math.Sin(elev);
            return new Vec3(x, y, z);
        }

        static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }
    }
}
