using System;
using System.Collections.Generic;

namespace CricketBlitz.Save
{
    [Serializable]
    public class SettingsData
    {
        public float music = 0.55f;
        public float sfx = 0.9f;
        public bool haptics = true;
        public int quality;
        public int frameCap = 60;
        public int controlScheme;
        public bool showFps;
    }

    /// <summary>
    /// Local career. JsonUtility writes the public fields.
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public string userName = "Guest";
        public bool isGuest = true;
        public bool accountChosen;
        public bool consentAccepted;
        public bool adsPersonalized;
        public bool locationAsked;
        public bool locationGranted;
        public int coins = 500;
        public int tickets = 2;
        public int energy = 10;
        public int energyMax = 10;
        public long lastEnergyUnix;
        public int level = 1;
        public int xpIntoLevel;
        public int lifetimeXp;
        public string avatarId = "teal";
        public bool blitzPass;
        public bool tutorialComplete;
        public int tutorialCoins;
        public int matchesPlayed;
        public int wins;
        public int bestScore;
        public long lastDailyUnix;
        public List<string> unlockedStadiums = new List<string>();
        public List<string> unlockedOvers = new List<string>();
        public List<string> unlockedKits = new List<string>();
        public SettingsData settings = new SettingsData();

        public static PlayerProfile CreateNew()
        {
            PlayerProfile profile = new PlayerProfile();
            profile.unlockedStadiums.Add("harbor_oval");
            profile.unlockedOvers.Add("2");
            profile.unlockedOvers.Add("5");
            profile.unlockedKits.Add("home");
            profile.lastEnergyUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return profile;
        }

        public void Repair()
        {
            if (string.IsNullOrEmpty(userName))
            {
                userName = "Guest";
            }

            if (settings == null)
            {
                settings = new SettingsData();
            }

            if (unlockedStadiums == null)
            {
                unlockedStadiums = new List<string>();
            }

            if (unlockedOvers == null)
            {
                unlockedOvers = new List<string>();
            }

            if (unlockedKits == null)
            {
                unlockedKits = new List<string>();
            }

            if (!unlockedStadiums.Contains("harbor_oval"))
            {
                unlockedStadiums.Add("harbor_oval");
            }

            if (!unlockedOvers.Contains("2"))
            {
                unlockedOvers.Add("2");
            }

            if (!unlockedOvers.Contains("5"))
            {
                unlockedOvers.Add("5");
            }

            if (energyMax <= 0)
            {
                energyMax = 10;
            }

            if (level <= 0)
            {
                level = 1;
            }

            if (frameCapInvalid())
            {
                settings.frameCap = 60;
            }
        }

        bool frameCapInvalid()
        {
            return settings.frameCap != 30 && settings.frameCap != 60;
        }

        public bool HasStadium(string id)
        {
            return unlockedStadiums.Contains(id);
        }

        public bool HasOvers(int overs)
        {
            return unlockedOvers.Contains(overs.ToString());
        }

        public void UnlockStadium(string id)
        {
            if (!unlockedStadiums.Contains(id))
            {
                unlockedStadiums.Add(id);
            }
        }

        public void UnlockOvers(int overs)
        {
            string key = overs.ToString();
            if (!unlockedOvers.Contains(key))
            {
                unlockedOvers.Add(key);
            }
        }
    }
}
