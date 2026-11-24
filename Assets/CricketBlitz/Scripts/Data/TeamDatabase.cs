using System.Collections.Generic;
using CricketBlitz.Sim;
using UnityEngine;

namespace CricketBlitz.Data
{
    public sealed class PlayerSeed
    {
        public string Name;
        public bool RightHanded = true;
        public int Technique = 60;
        public int Timing = 60;
        public BatsmanType Type = BatsmanType.Balanced;
        public int Bat = 50;
        public int Bowl = 20;
        public int Field = 55;
        public bool Keeper;
        public DeliveryType Stock = DeliveryType.Straight;
        public float Speed = 125f;
        public bool LeftArm;
    }

    public sealed class TeamSheet
    {
        public string Id;
        public string Name;
        public string ShortName;
        public string Category;
        public Color Color;
        public Color Accent;
        public PlayerSeed[] Players;

        public int BatPercent
        {
            get { return Average(true); }
        }

        public int BowlPercent
        {
            get { return AverageBowl(); }
        }

        public int FieldPercent
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < Players.Length; i++)
                {
                    sum += Players[i].Field;
                }

                return sum / Players.Length;
            }
        }

        public BatterScore[] ToBatters()
        {
            BatterScore[] scores = new BatterScore[Players.Length];
            for (int i = 0; i < Players.Length; i++)
            {
                PlayerSeed seed = Players[i];
                BatterScore score = new BatterScore();
                score.Name = seed.Name;
                score.RightHanded = seed.RightHanded;
                score.Technique = seed.Technique;
                score.Timing = seed.Timing;
                score.Type = seed.Type;
                score.BattingRating = seed.Bat;
                score.BowlingRating = seed.Bowl;
                score.FieldingRating = seed.Field;
                score.IsKeeper = seed.Keeper;
                score.StockBall = seed.Stock;
                scores[i] = score;
            }

            return scores;
        }

        int Average(bool bat)
        {
            int sum = 0;
            for (int i = 0; i < Players.Length; i++)
            {
                sum += bat ? Players[i].Bat : Players[i].Field;
            }

            return sum / Players.Length;
        }

        int AverageBowl()
        {
            int sum = 0;
            int count = 0;
            for (int i = 0; i < Players.Length; i++)
            {
                if (Players[i].Bowl >= 45)
                {
                    sum += Players[i].Bowl;
                    count += 1;
                }
            }

            if (count == 0)
            {
                return 30;
            }

            return sum / count;
        }
    }

    public static class TeamDatabase
    {
        static TeamSheet[] cache;

        public static TeamSheet[] All()
        {
            if (cache == null)
            {
                cache = Build();
            }

            return cache;
        }

        public static TeamSheet Get(string id)
        {
            TeamSheet[] all = All();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].Id == id)
                {
                    return all[i];
                }
            }

            return all[0];
        }

        public static List<TeamSheet> ByCategory(string category)
        {
            List<TeamSheet> list = new List<TeamSheet>();
            TeamSheet[] all = All();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].Category == category)
                {
                    list.Add(all[i]);
                }
            }

            return list;
        }

        static TeamSheet[] Build()
        {
            return new[]
            {
                HarborKings(),
                RidgeFalcons(),
                Dockyard(),
                Highline()
            };
        }

        static PlayerSeed P(
            string name,
            bool right,
            int tech,
            int timing,
            BatsmanType type,
            int bat,
            int bowl,
            int field,
            bool keeper,
            DeliveryType stock,
            float speed,
            bool leftArm)
        {
            PlayerSeed seed = new PlayerSeed();
            seed.Name = name;
            seed.RightHanded = right;
            seed.Technique = tech;
            seed.Timing = timing;
            seed.Type = type;
            seed.Bat = bat;
            seed.Bowl = bowl;
            seed.Field = field;
            seed.Keeper = keeper;
            seed.Stock = stock;
            seed.Speed = speed;
            seed.LeftArm = leftArm;
            return seed;
        }

        static TeamSheet HarborKings()
        {
            TeamSheet team = new TeamSheet();
            team.Id = "harbor_kings";
            team.Name = "Harbor Kings";
            team.ShortName = "HBR";
            team.Category = "International";
            team.Color = new Color(0.08f, 0.45f, 0.48f);
            team.Accent = new Color(0.93f, 0.76f, 0.28f);
            team.Players = new[]
            {
                P("Adrian Cole", true, 78, 82, BatsmanType.Balanced, 84, 22, 70, false, DeliveryType.Straight, 110f, false),
                P("Bilal Shah", false, 70, 86, BatsmanType.Radical, 80, 28, 74, false, DeliveryType.Straight, 112f, false),
                P("Mateo Lang", true, 62, 68, BatsmanType.Brute, 76, 35, 60, false, DeliveryType.Slower, 118f, false),
                P("Soren Hale", true, 84, 74, BatsmanType.Defensive, 73, 40, 72, false, DeliveryType.OffCutter, 115f, false),
                P("Idris Quay", true, 75, 77, BatsmanType.Balanced, 78, 48, 80, false, DeliveryType.OutSwing, 122f, false),
                P("Priya Raman", true, 66, 70, BatsmanType.Balanced, 64, 18, 86, true, DeliveryType.Straight, 100f, false),
                P("Jonah Voss", true, 48, 52, BatsmanType.Brute, 42, 78, 68, false, DeliveryType.OutSwing, 136f, false),
                P("Caleb Nyx", true, 44, 50, BatsmanType.Balanced, 28, 84, 62, false, DeliveryType.InSwing, 142f, false),
                P("Omar Estevez", true, 55, 58, BatsmanType.Defensive, 36, 80, 70, false, DeliveryType.OffCutter, 118f, false),
                P("Felix Drum", false, 40, 46, BatsmanType.Radical, 24, 76, 58, false, DeliveryType.LegCutter, 128f, true),
                P("Nate Okonkwo", true, 50, 54, BatsmanType.Balanced, 22, 82, 64, false, DeliveryType.LegCutter, 112f, false)
            };
            return team;
        }

        static TeamSheet RidgeFalcons()
        {
            TeamSheet team = new TeamSheet();
            team.Id = "ridge_falcons";
            team.Name = "Ridge Falcons";
            team.ShortName = "RDF";
            team.Category = "International";
            team.Color = new Color(0.62f, 0.24f, 0.18f);
            team.Accent = new Color(0.96f, 0.93f, 0.84f);
            team.Players = new[]
            {
                P("Harun Blake", true, 80, 76, BatsmanType.Balanced, 82, 20, 68, false, DeliveryType.Straight, 108f, false),
                P("Eddie Cho", false, 68, 84, BatsmanType.Radical, 79, 30, 77, false, DeliveryType.Slower, 114f, false),
                P("Marcus Pell", true, 60, 64, BatsmanType.Brute, 77, 32, 58, false, DeliveryType.Straight, 116f, false),
                P("Yusuf Dar", true, 83, 72, BatsmanType.Defensive, 74, 44, 75, false, DeliveryType.OffCutter, 120f, false),
                P("Leon Marsh", true, 74, 75, BatsmanType.Balanced, 76, 55, 82, false, DeliveryType.OutSwing, 126f, false),
                P("Tara Quinn", false, 64, 69, BatsmanType.Balanced, 62, 16, 88, true, DeliveryType.Straight, 100f, false),
                P("Samir Holt", true, 46, 50, BatsmanType.Brute, 38, 81, 66, false, DeliveryType.InSwing, 140f, false),
                P("Boyd Keller", true, 42, 48, BatsmanType.Balanced, 26, 79, 60, false, DeliveryType.ReverseSwing, 138f, false),
                P("Ingrid Moss", true, 52, 56, BatsmanType.Defensive, 34, 77, 73, false, DeliveryType.OffCutter, 116f, false),
                P("Paulo Serra", true, 48, 52, BatsmanType.Radical, 30, 83, 64, false, DeliveryType.LegCutter, 114f, false),
                P("Nico Adler", false, 38, 44, BatsmanType.Balanced, 20, 75, 61, false, DeliveryType.OutSwing, 132f, true)
            };
            return team;
        }

        static TeamSheet Dockyard()
        {
            TeamSheet team = new TeamSheet();
            team.Id = "dockyard_xi";
            team.Name = "Dockyard XI";
            team.ShortName = "DXI";
            team.Category = "Club";
            team.Color = new Color(0.13f, 0.22f, 0.4f);
            team.Accent = new Color(0.95f, 0.55f, 0.2f);
            team.Players = new[]
            {
                P("Ellis Ward", true, 64, 66, BatsmanType.Balanced, 68, 25, 60, false, DeliveryType.Straight, 108f, false),
                P("Noor Fadil", false, 60, 72, BatsmanType.Radical, 66, 22, 64, false, DeliveryType.Straight, 110f, false),
                P("Chris Pye", true, 55, 58, BatsmanType.Brute, 70, 30, 52, false, DeliveryType.Slower, 112f, false),
                P("Helen Cho", true, 72, 64, BatsmanType.Defensive, 63, 36, 66, false, DeliveryType.OffCutter, 114f, false),
                P("Ruben Salt", true, 66, 68, BatsmanType.Balanced, 67, 50, 70, false, DeliveryType.OutSwing, 124f, false),
                P("Omar Briggs", true, 58, 60, BatsmanType.Balanced, 55, 18, 74, true, DeliveryType.Straight, 100f, false),
                P("Pete Dalloway", true, 40, 44, BatsmanType.Brute, 32, 72, 58, false, DeliveryType.InSwing, 132f, false),
                P("Lila Venn", false, 42, 46, BatsmanType.Balanced, 28, 70, 62, false, DeliveryType.OutSwing, 128f, true),
                P("Gus Romero", true, 48, 50, BatsmanType.Defensive, 30, 74, 60, false, DeliveryType.OffCutter, 118f, false),
                P("Ian Brooks", true, 36, 40, BatsmanType.Radical, 22, 68, 55, false, DeliveryType.LegCutter, 122f, false),
                P("Sam Quill", true, 44, 48, BatsmanType.Balanced, 24, 71, 57, false, DeliveryType.Slower, 126f, false)
            };
            return team;
        }

        static TeamSheet Highline()
        {
            TeamSheet team = new TeamSheet();
            team.Id = "highline_cc";
            team.Name = "Highline CC";
            team.ShortName = "HLC";
            team.Category = "Club";
            team.Color = new Color(0.18f, 0.38f, 0.22f);
            team.Accent = new Color(0.93f, 0.9f, 0.7f);
            team.Players = new[]
            {
                P("Maya Trent", true, 70, 68, BatsmanType.Balanced, 71, 24, 66, false, DeliveryType.Straight, 108f, false),
                P("Jon Abebe", false, 62, 74, BatsmanType.Radical, 69, 26, 70, false, DeliveryType.Straight, 112f, false),
                P("Felix Barr", true, 58, 60, BatsmanType.Brute, 72, 28, 54, false, DeliveryType.Slower, 116f, false),
                P("Ruth Keene", true, 74, 66, BatsmanType.Defensive, 65, 34, 68, false, DeliveryType.OffCutter, 114f, false),
                P("Owen Pike", true, 68, 70, BatsmanType.Balanced, 70, 52, 76, false, DeliveryType.OutSwing, 125f, false),
                P("Ned Aoki", true, 60, 62, BatsmanType.Balanced, 58, 20, 80, true, DeliveryType.Straight, 100f, false),
                P("Carl Ibanez", true, 42, 46, BatsmanType.Brute, 30, 73, 60, false, DeliveryType.InSwing, 134f, false),
                P("Hope Daley", true, 40, 44, BatsmanType.Balanced, 26, 69, 63, false, DeliveryType.ReverseSwing, 130f, false),
                P("Yuri Pell", true, 50, 52, BatsmanType.Defensive, 32, 75, 67, false, DeliveryType.LegCutter, 112f, false),
                P("Ben Solt", false, 38, 42, BatsmanType.Radical, 22, 70, 58, false, DeliveryType.OutSwing, 129f, true),
                P("Tara Singh", true, 46, 50, BatsmanType.Balanced, 24, 72, 61, false, DeliveryType.OffCutter, 120f, false)
            };
            return team;
        }
    }
}
