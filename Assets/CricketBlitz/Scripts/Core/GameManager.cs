using System;
using CricketBlitz.Audio;
using CricketBlitz.Save;
using CricketBlitz.Services;
using UnityEngine;

namespace CricketBlitz.Core
{
    /// <summary>
    /// Career, settings and the 60 fps mobile defaults. Lives for the whole session.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public PlayerProfile Profile;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Profile = SaveSystem.Load();
            RegenEnergy();
            ApplySettings();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Save();
            }
        }

        void OnApplicationQuit()
        {
            Save();
        }

        public void ApplySettings()
        {
            int cap = Profile.settings.frameCap == 30 ? 30 : 60;
            Profile.settings.frameCap = cap;
            Application.targetFrameRate = cap;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 0;
            QualitySettings.shadows = Profile.settings.quality <= 0 ? ShadowQuality.Disable : ShadowQuality.HardOnly;
            QualitySettings.pixelLightCount = Profile.settings.quality <= 0 ? 1 : 2;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            if (Screen.orientation != ScreenOrientation.LandscapeLeft && Screen.orientation != ScreenOrientation.LandscapeRight)
            {
                Screen.orientation = ScreenOrientation.LandscapeLeft;
            }

            Haptics.Enabled = Profile.settings.haptics;
            if (MatchAudio.Instance != null)
            {
                MatchAudio.Instance.ApplyVolumes(Profile.settings.music, Profile.settings.sfx);
            }
        }

        public void Save()
        {
            SaveSystem.Save(Profile);
        }

        public void AddCoins(int amount)
        {
            Profile.coins = Mathf.Max(0, Profile.coins + amount);
        }

        public bool TrySpend(int amount)
        {
            if (Profile.coins < amount)
            {
                return false;
            }

            Profile.coins -= amount;
            Save();
            return true;
        }

        public int AddXp(int amount)
        {
            if (amount < 0)
            {
                amount = 0;
            }

            Profile.lifetimeXp += amount;
            Profile.xpIntoLevel += amount;
            int gained = 0;
            while (Profile.xpIntoLevel >= 100)
            {
                Profile.xpIntoLevel -= 100;
                Profile.level += 1;
                Profile.coins += 50;
                gained += 1;
            }

            return gained;
        }

        public void RegenEnergy()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (Profile.lastEnergyUnix <= 0)
            {
                Profile.lastEnergyUnix = now;
            }

            long elapsed = now - Profile.lastEnergyUnix;
            int gain = (int)(elapsed / 480L);
            if (gain > 0 && Profile.energy < Profile.energyMax)
            {
                Profile.energy = Mathf.Min(Profile.energyMax, Profile.energy + gain);
                Profile.lastEnergyUnix += gain * 480L;
            }
        }

        public bool TrySpendEnergy(int amount)
        {
            RegenEnergy();
            if (Profile.energy < amount)
            {
                return false;
            }

            Profile.energy -= amount;
            Profile.lastEnergyUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Save();
            return true;
        }

        public bool DailyReady()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return now - Profile.lastDailyUnix > 22L * 3600L;
        }

        public void ClaimDaily()
        {
            if (!DailyReady())
            {
                return;
            }

            Profile.lastDailyUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            AddCoins(100);
            Profile.tickets += 1;
            Save();
        }
    }
}
