using System;
using System.IO;
using UnityEngine;

namespace PlagueGame.Clinic
{
    public static class ClinicSaveStore
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "clinic-v1.json");

        public static ClinicState Load()
        {
            foreach (var path in new[] { SavePath, SavePath + ".bak" })
            {
                if (!File.Exists(path)) continue;
                try
                {
                    var state = JsonUtility.FromJson<ClinicState>(File.ReadAllText(path));
                    if (state != null && state.IsValid()) return state;
                    Debug.LogWarning("Clinic save did not pass validation; trying backup.");
                }
                catch (Exception exception) when (exception is IOException || exception is ArgumentException || exception is UnauthorizedAccessException)
                {
                    Debug.LogWarning("Clinic save could not be read; trying backup.");
                }
            }
            return ClinicState.NewGame();
        }

        public static void Save(ClinicState state)
        {
            if (state == null || !state.IsValid()) return;
            var path = SavePath;
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                var temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(state));
                if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
                else File.Move(temp, path);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning("Clinic progress could not be saved: " + exception.GetType().Name);
            }
        }
    }
}
