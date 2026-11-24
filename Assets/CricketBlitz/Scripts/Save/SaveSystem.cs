using System;
using System.IO;
using UnityEngine;

namespace CricketBlitz.Save
{
    /// <summary>
    /// Writes the career to disk and mirrors it to a second file.
    /// The mirror is the stand-in for a cloud save: replace CloudMirror with a
    /// signed-in backend later and the rest of the game can stay as it is.
    /// </summary>
    public static class SaveSystem
    {
        const string FileName = "cricket_blitz_profile.json";
        const string CloudName = "cricket_blitz_profile.cloud.json";

        public static string LocalPath
        {
            get { return Path.Combine(Application.persistentDataPath, FileName); }
        }

        public static string CloudPath
        {
            get { return Path.Combine(Application.persistentDataPath, CloudName); }
        }

        public static PlayerProfile Load()
        {
            PlayerProfile local = Read(LocalPath);
            PlayerProfile cloud = Read(CloudPath);
            PlayerProfile best = PickNewer(local, cloud);
            if (best == null)
            {
                best = PlayerProfile.CreateNew();
                Save(best);
            }
            else
            {
                best.Repair();
            }

            return best;
        }

        public static void Save(PlayerProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            profile.Repair();
            string json = JsonUtility.ToJson(profile, true);
            Write(LocalPath, json);
            Write(CloudPath, json);
        }

        static PlayerProfile PickNewer(PlayerProfile local, PlayerProfile cloud)
        {
            if (local == null)
            {
                return cloud;
            }

            if (cloud == null)
            {
                return local;
            }

            int localScore = local.lifetimeXp + local.matchesPlayed * 10 + local.level * 100;
            int cloudScore = cloud.lifetimeXp + cloud.matchesPlayed * 10 + cloud.level * 100;
            return cloudScore > localScore ? cloud : local;
        }

        static PlayerProfile Read(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                string json = File.ReadAllText(path);
                if (string.IsNullOrEmpty(json))
                {
                    return null;
                }

                return JsonUtility.FromJson<PlayerProfile>(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Cricket Blitz could not read " + path + ": " + exception.Message);
                return null;
            }
        }

        static void Write(string path, string json)
        {
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(path, json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Cricket Blitz could not write " + path + ": " + exception.Message);
            }
        }
    }
}
