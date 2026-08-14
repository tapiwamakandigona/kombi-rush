using System;
using System.IO;
using KombiRush.Sim;
using UnityEngine;

namespace KombiRush.Game
{
    /// <summary>
    /// Profile persistence. Writes to a temp file and moves it into place so a kill mid-write
    /// (very common on cheap Android devices) cannot leave a half-written save behind.
    /// </summary>
    public static class SaveIO
    {
        private const string FileName = "profile.txt";

        private static string Path => System.IO.Path.Combine(Application.persistentDataPath, FileName);

        public static Profile Load()
        {
            try
            {
                if (!File.Exists(Path)) return new Profile();
                return Profile.Deserialize(File.ReadAllText(Path));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[KombiRush] could not read save, starting fresh: " + ex.Message);
                return new Profile();
            }
        }

        public static void Save(Profile profile)
        {
            if (profile == null) return;
            try
            {
                string dir = Application.persistentDataPath;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string temp = Path + ".tmp";
                File.WriteAllText(temp, profile.Serialize());
                if (File.Exists(Path)) File.Delete(Path);
                File.Move(temp, Path);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[KombiRush] could not write save: " + ex.Message);
            }
        }

        /// <summary>Days since 1970, used for the once-a-day bonus without needing a server.</summary>
        public static int TodayIndex()
        {
            return (int)(DateTime.UtcNow.Date - new DateTime(1970, 1, 1)).TotalDays;
        }
    }
}
