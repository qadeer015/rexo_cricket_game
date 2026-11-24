using CricketBlitz.Bowling;
using CricketBlitz.World;
using UnityEngine;

namespace CricketBlitz.Fielding
{
    /// <summary>
    /// Places the nine fielders and walks the nearest one toward the ball.
    /// Catches are a distance test while the ball is still in the air.
    /// </summary>
    public sealed class FielderAI : MonoBehaviour
    {
        public PlayerAvatar[] Fielders;
        public PlayerAvatar Keeper;
        public PlayerAvatar Bowler;
        public Vector3[] Spots = new Vector3[9];
        public int Chaser = -1;
        public bool Holding;

        static readonly Vector3[] Attack =
        {
            new Vector3(1.5f, 0f, 10.3f),
            new Vector3(6f, 0f, 8.2f),
            new Vector3(15f, 0f, 3f),
            new Vector3(14f, 0f, -8f),
            new Vector3(7f, 0f, -16f),
            new Vector3(-7f, 0f, -16f),
            new Vector3(-14f, 0f, -2f),
            new Vector3(-15f, 0f, 7f),
            new Vector3(-7f, 0f, 13f)
        };

        static readonly Vector3[] Defend =
        {
            new Vector3(2.2f, 0f, 11f),
            new Vector3(24f, 0f, 4f),
            new Vector3(30f, 0f, -12f),
            new Vector3(12f, 0f, -30f),
            new Vector3(-12f, 0f, -30f),
            new Vector3(-30f, 0f, -10f),
            new Vector3(-24f, 0f, 8f),
            new Vector3(-18f, 0f, 22f),
            new Vector3(22f, 0f, 20f)
        };

        public void Bind(PlayerAvatar[] fielders, PlayerAvatar keeper, PlayerAvatar bowler)
        {
            Fielders = fielders;
            Keeper = keeper;
            Bowler = bowler;
            Place(FieldPlan.Balanced, false);
        }

        public void Place(FieldPlan plan, bool powerplay)
        {
            float weight = BowlingController.AttackWeight(plan);
            for (int i = 0; i < Spots.Length; i++)
            {
                Vector3 spot = Vector3.Lerp(Defend[i], Attack[i], weight);
                if (powerplay && spot.magnitude > 30f && i > 1)
                {
                    spot = spot.normalized * 28f;
                }

                Spots[i] = spot;
                if (Fielders != null && i < Fielders.Length && Fielders[i] != null)
                {
                    StadiumBuilder.SetXZ(Fielders[i], spot.x, spot.z);
                    StadiumBuilder.Face(Fielders[i], new Vector3(-spot.x, 0f, 8f - spot.z));
                }
            }

            Chaser = -1;
            Holding = false;
        }

        public void MoveFielder(int index, Vector3 world)
        {
            if (index < 0 || index >= Spots.Length)
            {
                return;
            }

            Vector3 flat = new Vector3(world.x, 0f, world.z);
            float radius = flat.magnitude;
            if (radius < 8f)
            {
                flat = flat.normalized * 8f;
            }

            if (radius > 52f)
            {
                flat = flat.normalized * 52f;
            }

            Spots[index] = flat;
            if (Fielders != null && Fielders[index] != null)
            {
                StadiumBuilder.SetXZ(Fielders[index], flat.x, flat.z);
            }
        }

        public void Tick(float dt, Vector3 ball, bool chase)
        {
            if (Fielders == null)
            {
                return;
            }

            if (chase && Chaser < 0)
            {
                Chaser = NearestIndex(ball);
            }

            if (!chase)
            {
                Chaser = -1;
            }

            float speed = 7.4f;
            for (int i = 0; i < Fielders.Length; i++)
            {
                if (Fielders[i] == null)
                {
                    continue;
                }

                Vector3 target = i == Chaser ? new Vector3(ball.x, 0f, ball.z) : Spots[i];
                Vector3 current = Fielders[i].transform.localPosition;
                Vector3 next = Vector3.MoveTowards(new Vector3(current.x, 0f, current.z), target, speed * dt);
                StadiumBuilder.SetXZ(Fielders[i], next.x, next.z);
                Fielders[i].SetRunning(i == Chaser && chase);
                if ((target - next).sqrMagnitude > 0.25f)
                {
                    StadiumBuilder.Face(Fielders[i], target - next);
                }
            }
        }

        public bool TryCatch(Vector3 ball, bool bouncedSinceHit, bool edge, float sinceHit, out PlayerAvatar who)
        {
            who = null;
            if (bouncedSinceHit || sinceHit < 0.12f || ball.y < 0.35f || ball.y > 3.4f)
            {
                return false;
            }

            if (Keeper != null)
            {
                float keeperReach = edge ? 2.4f : 1.3f;
                if (Distance(Keeper, ball) < keeperReach && ball.z > 8f)
                {
                    who = Keeper;
                    return true;
                }
            }

            if (sinceHit < 0.2f)
            {
                return false;
            }

            for (int i = 0; i < Fielders.Length; i++)
            {
                if (Fielders[i] != null && Distance(Fielders[i], ball) < 1.15f)
                {
                    who = Fielders[i];
                    return true;
                }
            }

            if (Bowler != null && Distance(Bowler, ball) < 1.15f && ball.z < 6f)
            {
                who = Bowler;
                return true;
            }

            return false;
        }

        public PlayerAvatar Nearest(Vector3 ball)
        {
            int index = NearestIndex(ball);
            if (index >= 0)
            {
                return Fielders[index];
            }

            return Bowler;
        }

        int NearestIndex(Vector3 ball)
        {
            int best = -1;
            float bestDistance = 100000f;
            for (int i = 0; i < Fielders.Length; i++)
            {
                if (Fielders[i] == null)
                {
                    continue;
                }

                float distance = Distance(Fielders[i], ball);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }

        static float Distance(PlayerAvatar avatar, Vector3 ball)
        {
            Vector3 position = avatar.transform.position;
            float dx = position.x - ball.x;
            float dz = position.z - ball.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
