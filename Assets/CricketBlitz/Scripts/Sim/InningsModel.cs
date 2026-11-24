using System;

namespace CricketBlitz.Sim
{
    public sealed class BatterScore
    {
        public string Name = string.Empty;
        public bool RightHanded = true;
        public int Technique = 50;
        public int Timing = 50;
        public BatsmanType Type = BatsmanType.Balanced;
        public int BattingRating = 50;
        public int BowlingRating = 20;
        public int FieldingRating = 50;
        public bool IsKeeper;
        public DeliveryType StockBall = DeliveryType.Straight;
        public int Runs;
        public int Balls;
        public bool Out;
        public bool HasBatted;
        public int Fours;
        public int Sixes;
    }

    public sealed class BowlerSpell
    {
        public string Name = string.Empty;
        public int PlayerIndex;
        public int LegalBalls;
        public int RunsConceded;
        public int Wickets;
        public int Maidens;
        public int RunsThisOver;
    }

    public struct BallRecord
    {
        public int BatRuns;
        public int ExtraRuns;
        public ExtraKind Extra;
        public bool Wicket;
        public WicketKind WicketKind;
        public bool OutIsStriker;
        public bool FreeHitActive;
        public string Symbol;

        public int TotalRuns
        {
            get { return BatRuns + ExtraRuns; }
        }
    }

    /// <summary>
    /// Laws used by the MVP: legal balls, strike rotation, wides, no-balls,
    /// free hits, maidens, all-out and a chase.
    /// </summary>
    public sealed class InningsModel
    {
        public string BattingTeam = string.Empty;
        public string BattingShort = string.Empty;
        public string BowlingTeam = string.Empty;
        public int TotalOvers = 5;
        public int MaxWickets = 10;
        public int Runs;
        public int Wickets;
        public int LegalBalls;
        public int Extras;
        public int Target = -1;
        public bool FreeHitPending;
        public bool InningsOver;
        public bool ChaseAchieved;
        public BatterScore[] Batters = new BatterScore[0];
        public int Striker;
        public int NonStriker;
        public BowlerSpell[] Bowlers = new BowlerSpell[0];
        public int CurrentBowler;
        public string[] LastSix = new string[6];
        public int LastSixCount;

        public int CompletedOvers
        {
            get { return LegalBalls / 6; }
        }

        public int BallsIntoOver
        {
            get { return LegalBalls % 6; }
        }

        public string OversText
        {
            get { return CompletedOvers + "." + BallsIntoOver; }
        }

        public bool IsPowerplay
        {
            get { return CompletedOvers < PowerplayOvers(TotalOvers) && !InningsOver; }
        }

        public BatterScore StrikerScore
        {
            get { return Batters[Striker]; }
        }

        public BowlerSpell ActiveBowler
        {
            get { return Bowlers[CurrentBowler]; }
        }

        public static int PowerplayOvers(int totalOvers)
        {
            if (totalOvers <= 5)
            {
                return 1;
            }

            if (totalOvers <= 10)
            {
                return 2;
            }

            if (totalOvers <= 20)
            {
                return 6;
            }

            return 10;
        }

        public void SetOpeners(int striker, int nonStriker)
        {
            Striker = striker;
            NonStriker = nonStriker;
            Batters[striker].HasBatted = true;
            Batters[nonStriker].HasBatted = true;
        }

        public bool FreeHitForNextBall()
        {
            return FreeHitPending;
        }

        public void ConsumeFreeHitFlag()
        {
            FreeHitPending = false;
        }

        public static bool WicketCounts(BallRecord record)
        {
            if (!record.Wicket || record.WicketKind == WicketKind.None)
            {
                return false;
            }

            bool illegal = record.Extra == ExtraKind.Wide || record.Extra == ExtraKind.NoBall;
            if (record.WicketKind != WicketKind.RunOut && (record.FreeHitActive || illegal))
            {
                return false;
            }

            return true;
        }

        public void Commit(BallRecord record)
        {
            if (InningsOver)
            {
                return;
            }

            int facing = Striker;
            bool legal = record.Extra != ExtraKind.Wide && record.Extra != ExtraKind.NoBall;
            bool wicketCounts = WicketCounts(record);

            Runs += record.TotalRuns;
            Extras += record.ExtraRuns;

            BowlerSpell bowler = Bowlers[CurrentBowler];
            bowler.RunsConceded += record.TotalRuns;
            bowler.RunsThisOver += record.TotalRuns;

            // Bowled and caught wipe the runs. A run-out keeps the runs already completed.
            bool creditBatter = record.BatRuns > 0
                && !(wicketCounts && record.WicketKind != WicketKind.RunOut);
            if (creditBatter)
            {
                BatterScore batter = Batters[facing];
                batter.Runs += record.BatRuns;
                if (record.BatRuns == 4)
                {
                    batter.Fours += 1;
                }
                else if (record.BatRuns == 6)
                {
                    batter.Sixes += 1;
                }
            }

            if (legal)
            {
                LegalBalls += 1;
                bowler.LegalBalls += 1;
                Batters[facing].Balls += 1;
            }

            if (wicketCounts)
            {
                Wickets += 1;
                int outIndex = record.OutIsStriker ? facing : NonStriker;
                Batters[outIndex].Out = true;
                if (record.WicketKind != WicketKind.RunOut)
                {
                    bowler.Wickets += 1;
                }

                if (Wickets < MaxWickets)
                {
                    BringInNext(record.OutIsStriker);
                }
            }
            else
            {
                int rotating = record.BatRuns;
                if (record.Extra == ExtraKind.Bye)
                {
                    rotating += record.ExtraRuns;
                }

                if ((rotating % 2) == 1)
                {
                    SwapStrike();
                }
            }

            if (record.Extra == ExtraKind.NoBall)
            {
                FreeHitPending = true;
            }

            PushSymbol(MakeSymbol(record));

            bool endOfOver = legal && LegalBalls % 6 == 0 && LegalBalls > 0;
            if (endOfOver)
            {
                if (bowler.RunsThisOver == 0)
                {
                    bowler.Maidens += 1;
                }

                bowler.RunsThisOver = 0;
                SwapStrike();
            }

            if (Wickets >= MaxWickets || LegalBalls >= TotalOvers * 6)
            {
                InningsOver = true;
            }

            if (Target > 0 && Runs >= Target)
            {
                ChaseAchieved = true;
                InningsOver = true;
            }
        }

        public void SwapStrike()
        {
            int swap = Striker;
            Striker = NonStriker;
            NonStriker = swap;
        }

        public string BowlerFigures()
        {
            BowlerSpell bowler = Bowlers[CurrentBowler];
            int overs = bowler.LegalBalls / 6;
            int balls = bowler.LegalBalls % 6;
            return bowler.Name + " " + bowler.Wickets + "-" + bowler.RunsConceded + " (" + overs + "." + balls + ")";
        }

        public static string MakeSymbol(BallRecord record)
        {
            if (WicketCounts(record))
            {
                return "W";
            }

            int n = record.BatRuns + record.ExtraRuns;
            if (record.Extra == ExtraKind.Wide)
            {
                return n <= 1 ? "Wd" : n + "Wd";
            }

            if (record.Extra == ExtraKind.NoBall)
            {
                return n <= 1 ? "Nb" : n + "Nb";
            }

            return n.ToString();
        }

        void BringInNext(bool strikerOut)
        {
            if (Wickets >= MaxWickets)
            {
                return;
            }

            int next = FindNextBatter();
            if (next < 0)
            {
                InningsOver = true;
                return;
            }

            Batters[next].HasBatted = true;
            if (strikerOut)
            {
                Striker = next;
            }
            else
            {
                NonStriker = next;
            }
        }

        int FindNextBatter()
        {
            for (int i = 0; i < Batters.Length; i++)
            {
                if (!Batters[i].HasBatted && !Batters[i].Out && i != Striker && i != NonStriker)
                {
                    return i;
                }
            }

            return -1;
        }

        void PushSymbol(string symbol)
        {
            if (LastSixCount < 6)
            {
                LastSix[LastSixCount] = symbol;
                LastSixCount += 1;
                return;
            }

            for (int i = 0; i < 5; i++)
            {
                LastSix[i] = LastSix[i + 1];
            }

            LastSix[5] = symbol;
        }
    }

    public static class DlsLite
    {
        /// <summary>
        /// Fraction of batting resources left in an uninterrupted innings of this length.
        /// Shorter innings keep a higher share of their closing overs, which is enough
        /// for a rain cut without a full Duckworth-Lewis table.
        /// </summary>
        public static float Resource(int overs)
        {
            if (overs <= 0)
            {
                return 0f;
            }

            double x = Math.Min(overs, 50) / 50.0;
            return (float)(Math.Pow(x, 0.78) * 100.0);
        }

        public static int RevisedTarget(int firstInningsScore, int firstInningsOvers, int secondInningsOvers)
        {
            float first = Resource(firstInningsOvers);
            float second = Resource(secondInningsOvers);
            if (first <= 0.01f)
            {
                return firstInningsScore + 1;
            }

            int par = (int)Math.Floor(firstInningsScore * (second / first));
            return par + 1;
        }
    }
}
