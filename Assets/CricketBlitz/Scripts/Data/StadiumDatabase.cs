using UnityEngine;

namespace CricketBlitz.Data
{
    public sealed class StadiumInfo
    {
        public string Id;
        public string Name;
        public string City;
        public int UnlockCost;
        public Color Outfield;
        public Color Stands;
        public Color Accent;
        public float WindKmh;
        public string[] Sponsors;
    }

    public static class StadiumDatabase
    {
        static StadiumInfo[] cache;

        public static StadiumInfo[] All()
        {
            if (cache == null)
            {
                cache = new[]
                {
                    Make("harbor_oval", "Harbor Oval", "Port Meridian", 0,
                        new Color(0.13f, 0.48f, 0.24f), new Color(0.12f, 0.16f, 0.24f), new Color(0.93f, 0.76f, 0.28f), 8f,
                        new[] { "Nimbus Cola", "PeakForge", "Lumen Bank", "Vivid Tel" }),
                    Make("north_wharf", "North Wharf", "Cape Solace", 4000,
                        new Color(0.16f, 0.42f, 0.32f), new Color(0.2f, 0.22f, 0.26f), new Color(0.4f, 0.75f, 0.72f), 14f,
                        new[] { "Orbit Grain", "Northwind Air", "Marble Mint", "Lumen Bank" }),
                    Make("lantern_park", "Lantern Park", "Old Kiln", 8000,
                        new Color(0.18f, 0.45f, 0.2f), new Color(0.28f, 0.16f, 0.14f), new Color(0.95f, 0.55f, 0.22f), 6f,
                        new[] { "Marble Mint", "PeakForge", "Harborline", "Vivid Tel" }),
                    Make("redrock_ground", "Redrock Ground", "Cinder Mesa", 15000,
                        new Color(0.34f, 0.4f, 0.18f), new Color(0.38f, 0.18f, 0.12f), new Color(0.86f, 0.32f, 0.22f), 11f,
                        new[] { "Cinder Rail", "Orbit Grain", "Nimbus Cola", "PeakForge" })
                };
            }

            return cache;
        }

        public static StadiumInfo Get(string id)
        {
            StadiumInfo[] all = All();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].Id == id)
                {
                    return all[i];
                }
            }

            return all[0];
        }

        static StadiumInfo Make(string id, string name, string city, int cost, Color outfield, Color stands, Color accent, float wind, string[] sponsors)
        {
            StadiumInfo info = new StadiumInfo();
            info.Id = id;
            info.Name = name;
            info.City = city;
            info.UnlockCost = cost;
            info.Outfield = outfield;
            info.Stands = stands;
            info.Accent = accent;
            info.WindKmh = wind;
            info.Sponsors = sponsors;
            return info;
        }
    }
}
