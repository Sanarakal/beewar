using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PlagueGame.Clinic
{
    [RequireComponent(typeof(PortraitLock))]
    public sealed class ClinicPrototype : MonoBehaviour
    {
        public Font uiFont;
        public Material worldMaterial;
        private ClinicSimulation simulation;
        private ClinicWorldView world;
        private ClinicHud hud;
        private float saveIn = 5;
        private float refreshIn;
        private int lastCured;
        private int lastBatches;

        private void Awake()
        {
            simulation = new ClinicSimulation(ClinicSaveStore.Load());
            lastCured = simulation.State.cured;
            lastBatches = simulation.State.batchesCrafted;
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("Event system", typeof(EventSystem), typeof(StandaloneInputModule));
            world = new GameObject("Clinic world").AddComponent<ClinicWorldView>();
            world.Initialize(simulation, worldMaterial);
            hud = new GameObject("Clinic interface", typeof(RectTransform)).AddComponent<ClinicHud>();
            hud.Initialize(simulation, uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), Perform);
            Debug.Log("PLAGUE_CLINIC_READY portrait " + Screen.width + "x" + Screen.height);
        }

        private void Update()
        {
            simulation.Tick(Mathf.Min(Time.deltaTime, .25f));
            world.Render(Time.deltaTime);
            if (simulation.State.cured > lastCured)
            {
                hud.ShowMessage("Пациент здоров!  +" + (18 + simulation.State.level * 2) + " монет");
                lastCured = simulation.State.cured;
                ClinicSaveStore.Save(simulation.State);
            }
            if (simulation.State.batchesCrafted > lastBatches)
            {
                hud.ShowMessage("Лекарства готовы!  +2 порции");
                lastBatches = simulation.State.batchesCrafted;
            }
            refreshIn -= Time.unscaledDeltaTime;
            if (refreshIn <= 0) { hud.Render(); refreshIn = .15f; }
            saveIn -= Time.unscaledDeltaTime;
            if (saveIn <= 0) { ClinicSaveStore.Save(simulation.State); saveIn = 5; }
        }

        private void Perform(Func<ClinicAction> action)
        {
            var result = action();
            hud.ShowMessage(result.message);
            hud.Render();
            if (result.success)
            {
                ClinicSaveStore.Save(simulation.State);
                Debug.Log("PLAGUE_CLINIC_ACTION " + result.message);
            }
        }

        private void OnApplicationPause(bool paused) { if (paused && simulation != null) ClinicSaveStore.Save(simulation.State); }
        private void OnApplicationQuit() { if (simulation != null) ClinicSaveStore.Save(simulation.State); }
    }
}
