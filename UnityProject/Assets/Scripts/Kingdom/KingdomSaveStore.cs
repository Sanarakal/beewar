using System;
using System.IO;
using UnityEngine;

namespace PlagueGame.Kingdom
{
    public static class KingdomSaveStore
    {
        [Serializable]
        private sealed class SaveData
        {
            public int format = 1;
            public bool hasUpgrade, hasTraining, hasMarch, hasReport;
            public KingdomState state;
        }
        public static string SavePath => Path.Combine(Application.persistentDataPath, "kingdom-v1.json");
        public static double UtcNow => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d;

        public static KingdomState Load()
        {
            foreach (var path in new[] { SavePath, SavePath + ".bak" })
            {
                if (!File.Exists(path)) continue;
                try
                {
                    var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                    var state = data != null && data.format == 1 ? data.state : null;
                    // JsonUtility materializes null inline classes as default objects on reload.
                    // Explicit presence flags preserve the distinction between no order and an active order.
                    if (state != null)
                    {
                        if (!data.hasUpgrade) state.upgrade = null;
                        if (!data.hasTraining) state.training = null;
                        if (!data.hasMarch) state.march = null;
                        if (!data.hasReport) state.report = null;
                    }
                    if (state != null && state.IsValid()) return state;
                    Debug.LogWarning("Kingdom save failed validation; checking backup.");
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
                { Debug.LogWarning("Kingdom save could not be read: " + exception.GetType().Name); }
            }
            return KingdomState.NewGame(UtcNow);
        }

        public static void Save(KingdomState state)
        {
            if (state == null || !state.IsValid()) { Debug.LogWarning("Invalid kingdom state was not saved."); return; }
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                var temp = SavePath + ".tmp";
                var data = new SaveData { state = state, hasUpgrade = state.upgrade != null, hasTraining = state.training != null,
                    hasMarch = state.march != null, hasReport = state.report != null };
                File.WriteAllText(temp, JsonUtility.ToJson(data));
                if (File.Exists(SavePath)) File.Replace(temp, SavePath, SavePath + ".bak");
                else File.Move(temp, SavePath);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            { Debug.LogWarning("Kingdom progress could not be saved: " + exception.GetType().Name); }
        }
    }
}
