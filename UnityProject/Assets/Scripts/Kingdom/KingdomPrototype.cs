using System;
using PlagueGame.Clinic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PlagueGame.Kingdom
{
    public enum KingdomTab { Settlement, Army, Map }

    [RequireComponent(typeof(PortraitLock))]
    public sealed class KingdomPrototype : MonoBehaviour
    {
        public Font uiFont;
        public Material worldMaterial;
        public KingdomSimulation Simulation { get; private set; }
        public KingdomHud Hud { get; private set; }
        private KingdomWorldView world;
        private float saveIn = 5, refreshIn;
        private int lastEvent;
        private bool pointerStarted, dragged;
        private Vector2 pointerOrigin, pointerPrevious;

        private void Awake()
        {
            Simulation = new KingdomSimulation(KingdomSaveStore.Load());
            Simulation.AdvanceTo(KingdomSaveStore.UtcNow);
            lastEvent = Simulation.EventVersion;
            if (FindFirstObjectByType<EventSystem>() == null) new GameObject("Event system", typeof(EventSystem), typeof(StandaloneInputModule));
            world = new GameObject("Kingdom world").AddComponent<KingdomWorldView>();
            world.Initialize(Simulation, worldMaterial);
            Hud = new GameObject("Kingdom interface", typeof(RectTransform)).AddComponent<KingdomHud>();
            Hud.Initialize(Simulation, uiFont, world, Perform, ChangeTab);
            KingdomSaveStore.Save(Simulation.State);
            Debug.Log("PLAGUE_KINGDOM_READY portrait " + Screen.width + "x" + Screen.height);
        }

        private void Update()
        {
            Simulation.AdvanceTo(KingdomSaveStore.UtcNow);
            world.Render(Hud.SelectedBuilding, Hud.SelectedMonster);
            Hud.PositionWorldLabels();
            HandlePointer();
            if (lastEvent != Simulation.EventVersion)
            {
                Hud.ShowMessage(Simulation.LastEvent);
                lastEvent = Simulation.EventVersion;
                KingdomSaveStore.Save(Simulation.State);
                Debug.Log("PLAGUE_KINGDOM_EVENT " + Simulation.LastEvent);
            }
            refreshIn -= Time.unscaledDeltaTime;
            if (refreshIn <= 0) { Hud.Render(); refreshIn = .15f; }
            saveIn -= Time.unscaledDeltaTime;
            if (saveIn <= 0) { KingdomSaveStore.Save(Simulation.State); saveIn = 5; }
        }

        private void Perform(Func<KingdomAction> command)
        {
            Simulation.AdvanceTo(KingdomSaveStore.UtcNow);
            var result = command();
            Hud.ShowMessage(result.message); Hud.Render();
            if (result.success) { KingdomSaveStore.Save(Simulation.State); Debug.Log("PLAGUE_KINGDOM_ACTION " + result.message); }
        }
        private void ChangeTab(KingdomTab tab) { world.SetTab(tab); }

        private void HandlePointer()
        {
            if (Hud.BlocksWorldInput) { pointerStarted = false; return; }
            if (Input.touchCount == 2)
            {
                var a = Input.GetTouch(0); var b = Input.GetTouch(1);
                if (!Camera.main.pixelRect.Contains(a.position) || !Camera.main.pixelRect.Contains(b.position)) return;
                var before = Vector2.Distance(a.position - a.deltaPosition, b.position - b.deltaPosition);
                world.Zoom((before - Vector2.Distance(a.position, b.position)) * .015f);
                pointerStarted = false; return;
            }
            if (Input.GetMouseButtonDown(0))
            {
                var id = Input.touchCount > 0 ? Input.GetTouch(0).fingerId : -1;
                pointerStarted = !EventSystem.current.IsPointerOverGameObject(id) && Camera.main.pixelRect.Contains(Input.mousePosition);
                pointerOrigin = pointerPrevious = Input.mousePosition; dragged = false;
            }
            if (pointerStarted && Input.GetMouseButton(0))
            {
                Vector2 current = Input.mousePosition;
                if (Vector2.Distance(current, pointerOrigin) > 15) dragged = true;
                if (dragged) world.Pan(current - pointerPrevious);
                pointerPrevious = current;
            }
            if (pointerStarted && Input.GetMouseButtonUp(0))
            {
                pointerStarted = false;
                if (dragged) return;
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit, 120))
                {
                    var target = hit.collider.GetComponent<KingdomTarget>();
                    if (target != null) { if (target.monster) Hud.SelectMonster(target.index); else Hud.SelectBuilding((BuildingKind)target.index); }
                }
            }
            if (Camera.main.pixelRect.Contains(Input.mousePosition)) world.Zoom(-Input.mouseScrollDelta.y);
        }

        private void OnApplicationPause(bool paused) { if (paused && Simulation != null) { Simulation.AdvanceTo(KingdomSaveStore.UtcNow); KingdomSaveStore.Save(Simulation.State); } }
        private void OnApplicationQuit() { if (Simulation != null) KingdomSaveStore.Save(Simulation.State); }
    }
}
