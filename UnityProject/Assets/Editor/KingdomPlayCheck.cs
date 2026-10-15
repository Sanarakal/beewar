using System;
using System.IO;
using PlagueGame.Kingdom;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PlagueGame.EditorTools
{
    // Runs actual HUD button callbacks against a fresh scene, preserving the editor's local save.
    [InitializeOnLoad]
    public static class KingdomPlayCheck
    {
        private const string Active = "KingdomPlayCheck.Active";
        private const string Done = "KingdomPlayCheck.Done";
        static KingdomPlayCheck() { EditorApplication.update += Update; }

        public static void Run()
        {
            ProjectBootstrap.ConfigureKingdom();
            SessionState.SetBool("KingdomPlayCheck.HadStoryPref", PlayerPrefs.HasKey("HiveStorySeen"));
            SessionState.SetInt("KingdomPlayCheck.StoryPref", PlayerPrefs.GetInt("HiveStorySeen", 0));
            foreach (var suffix in new[] { "", ".bak" })
            {
                var path = KingdomSaveStore.SavePath + suffix;
                SessionState.SetBool("KingdomPlayCheck.Exists" + suffix, File.Exists(path));
                SessionState.SetString("KingdomPlayCheck.Content" + suffix, File.Exists(path) ? File.ReadAllText(path) : "");
            }
            var initial = KingdomState.NewGame(KingdomSaveStore.UtcNow);
            initial.food = 321; initial.tier1 = 3;
            KingdomSaveStore.Save(initial);
            var reloaded = KingdomSaveStore.Load();
            Require(reloaded.food == 321 && reloaded.tier1 == 3 && reloaded.upgrade == null && reloaded.march == null, "Unity save round trip with absent orders");
            KingdomSaveStore.Save(KingdomState.NewGame(KingdomSaveStore.UtcNow));
            SessionState.SetBool(Active, true); SessionState.SetBool(Done, false);
            SessionState.SetInt("KingdomPlayCheck.Exit", 0);
            SessionState.SetFloat("KingdomPlayCheck.Deadline", (float)EditorApplication.timeSinceStartup + 90);
            EditorApplication.isPlaying = true;
        }

        private static void Update()
        {
            if (!SessionState.GetBool(Active, false)) return;
            if (SessionState.GetBool(Done, false))
            {
                if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
                RestoreAndExit(); return;
            }
            if (EditorApplication.timeSinceStartup > SessionState.GetFloat("KingdomPlayCheck.Deadline", 0)) { Finish(new Exception("Play check timed out")); return; }
            if (!EditorApplication.isPlaying || Time.frameCount < 8) return;
            var game = UnityEngine.Object.FindFirstObjectByType<KingdomPrototype>();
            if (game == null || game.Hud == null) return;
            try
            {
                var simulation = game.Simulation;
                Require(EventSystem.current != null, "event system");
                Require(game.uiFont != null && game.uiFont.HasCharacter('Я'), "Cyrillic font");
                Require(Camera.main.pixelRect.height > 0 && Camera.main.orthographic, "world camera");
                game.Hud.ShowStory();
                Require(GameObject.Find("Story overlay") != null && simulation.StoryStage == 0, "intro and story chapter");
                Click("Close story");
                Click("Upgrade building");
                Require(simulation.State.upgrade != null, "upgrade callback");
                simulation.AdvanceTo(simulation.State.upgrade.endsAt + .01); game.Hud.Render();
                Require(simulation.Level(BuildingKind.Castle) == 2, "castle completion");
                Click("Tab Army"); Click("Train soldiers");
                Require(simulation.State.training != null, "training callback");
                KingdomSaveStore.Save(simulation.State);
                var activeReload = KingdomSaveStore.Load();
                Require(activeReload.training != null && activeReload.training.count == 10 && activeReload.upgrade == null, "Unity save round trip with training");
                simulation.AdvanceTo(simulation.State.training.endsAt + .01); game.Hud.Render();
                Require(simulation.AvailableTroops == 10, "trained soldiers");
                Click("Tab Map"); Click("Attack monster");
                Require(simulation.State.march != null && simulation.AvailableTroops == 0, "march callback reserves army");
                KingdomSaveStore.Save(simulation.State);
                activeReload = KingdomSaveStore.Load();
                Require(activeReload.march != null && activeReload.march.id == simulation.State.march.id && activeReload.report == null, "Unity save round trip with march");
                simulation.AdvanceTo(simulation.State.march.homeAt + .01); game.Hud.Render();
                Require(simulation.State.report != null && simulation.State.report.won && simulation.State.report.delivered, "battle and delivered reward");
                Require(simulation.StoryStage == 3 && simulation.StoryText.Contains("печать"), "story waits for delivered victory");
                KingdomSaveStore.Save(simulation.State);
                activeReload = KingdomSaveStore.Load();
                Require(activeReload.report != null && activeReload.report.delivered && activeReload.march == null && activeReload.heroExperience == 20, "Unity save round trip with completed battle");
                Click("Battle report");
                Require(GameObject.Find("Report overlay") != null, "report opens");
                Click("Close report"); Click("Tab Settlement"); Click("Select Farm");
                Require(game.Hud.SelectedBuilding == BuildingKind.Farm, "building selection");
                Click("Upgrade building");
                Require(simulation.State.upgrade.building == BuildingKind.Farm && simulation.State.IsValid(), "selected building upgrade and valid state");
                Debug.Log("PLAGUE_KINGDOM_PLAY_CHECK_OK castle, training, march, battle, report, selection, font, Unity save round trips");
                Finish(null);
            }
            catch (Exception exception) { Finish(exception); }
        }

        private static void Click(string name)
        {
            foreach (var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (button.name != name) continue;
                Require(button.gameObject.activeInHierarchy && button.interactable, name + " available");
                button.onClick.Invoke(); return;
            }
            throw new Exception("Button missing: " + name);
        }
        private static void Require(bool condition, string name) { if (!condition) throw new Exception("Check failed: " + name); }
        private static void Finish(Exception exception)
        {
            if (exception != null) { Debug.LogException(exception); SessionState.SetInt("KingdomPlayCheck.Exit", 1); }
            SessionState.SetBool(Done, true); EditorApplication.isPlaying = false;
        }
        private static void RestoreAndExit()
        {
            foreach (var suffix in new[] { "", ".bak" })
            {
                var path = KingdomSaveStore.SavePath + suffix;
                if (SessionState.GetBool("KingdomPlayCheck.Exists" + suffix, false)) File.WriteAllText(path, SessionState.GetString("KingdomPlayCheck.Content" + suffix, ""));
                else if (File.Exists(path)) File.Delete(path);
            }
            SessionState.SetBool(Active, false);
            if (SessionState.GetBool("KingdomPlayCheck.HadStoryPref", false)) PlayerPrefs.SetInt("HiveStorySeen", SessionState.GetInt("KingdomPlayCheck.StoryPref", 0));
            else PlayerPrefs.DeleteKey("HiveStorySeen");
            PlayerPrefs.Save();
            EditorApplication.Exit(SessionState.GetInt("KingdomPlayCheck.Exit", 1));
        }
    }
}
