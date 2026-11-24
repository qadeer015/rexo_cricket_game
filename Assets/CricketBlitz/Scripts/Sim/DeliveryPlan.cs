namespace CricketBlitz.Sim
{
    /// <summary>
    /// One bowled delivery. Swing is signed in world X: positive moves toward +X,
    /// which is the off side for a right-handed batter facing +Z to -Z.
    /// </summary>
    public struct DeliveryPlan
    {
        public DeliveryType Type;
        public float SpeedKmh;
        public float LineX;
        public float BounceZ;
        public float Spin;
        public float Swing;
        public bool RoundTheWicket;
        public bool MarkedNoBall;
        public string BowlerName;

        public static DeliveryPlan Make(DeliveryType type, float speedKmh, float lineX, float bounceZ, bool roundTheWicket)
        {
            DeliveryPlan plan = new DeliveryPlan();
            plan.Type = type;
            plan.SpeedKmh = speedKmh;
            plan.LineX = lineX;
            plan.BounceZ = bounceZ;
            plan.RoundTheWicket = roundTheWicket;
            plan.BowlerName = string.Empty;
            ApplyType(ref plan);
            return plan;
        }

        public static void ApplyType(ref DeliveryPlan plan)
        {
            plan.Spin = 0f;
            plan.Swing = 0f;
            switch (plan.Type)
            {
                case DeliveryType.OutSwing:
                    plan.Swing = 1f;
                    break;
                case DeliveryType.InSwing:
                    plan.Swing = -1f;
                    break;
                case DeliveryType.ReverseSwing:
                    plan.Swing = -1f;
                    plan.SpeedKmh += 4f;
                    break;
                case DeliveryType.LegCutter:
                    plan.Spin = -1f;
                    break;
                case DeliveryType.OffCutter:
                    plan.Spin = 1f;
                    break;
                case DeliveryType.Slower:
                    plan.SpeedKmh *= 0.84f;
                    break;
            }

            if (plan.SpeedKmh < 70f)
            {
                plan.SpeedKmh = 70f;
            }

            if (plan.SpeedKmh > 155f)
            {
                plan.SpeedKmh = 155f;
            }
        }

        public float SpeedMs
        {
            get { return SpeedKmh / 3.6f; }
        }
    }
}
