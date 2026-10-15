using System;
using UnityEngine;
using UnityEngine.UI;

namespace PlagueGame.Clinic
{
    public sealed class ClinicHud : MonoBehaviour
    {
        private ClinicSimulation simulation;
        private Font font;
        private Text coinValue, herbValue, medicineValue, levelLabel, objective, objectiveHint, status, toastLabel;
        private Button admit, gather, craft, buyBed, upgrade;
        private Text admitLabel, gatherLabel, craftLabel, bedLabel, upgradeLabel;
        private GameObject toast;
        private float toastUntil;
        private Sprite panelSprite;
        private Texture2D panelTexture;
        private readonly Color ink = ColorHex("#304B48");
        private readonly Color secondary = ColorHex("#6C7A72");

        public void Initialize(ClinicSimulation clinic, Font uiFont, Action<Func<ClinicAction>> perform)
        {
            simulation = clinic;
            font = uiFont;
            CreatePanelSprite();
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 2400);
            scaler.matchWidthOrHeight = 0;
            gameObject.AddComponent<GraphicRaycaster>();
            var safe = Rect("Safe area", transform);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            var header = Rect("Header", safe);
            Top(header, 22, 90);
            Label(header, "ЛЕЧЕБНИЦА", 22, secondary, TextAnchor.UpperLeft, 0, 0, -150, 32);
            Label(header, "Первый день", 46, ink, TextAnchor.UpperLeft, 0, 30, -150, 60);
            levelLabel = Label(header, "Врач · 1 ур.", 27, ink, TextAnchor.MiddleRight, 0, 10, 0, 70);
            levelLabel.rectTransform.anchorMin = new Vector2(.65f, 1);

            var resources = Rect("Resources", safe);
            Top(resources, 128, 105);
            coinValue = Resource(resources, 0, "МОНЕТЫ", "#EEE3CB");
            herbValue = Resource(resources, 1, "ТРАВЫ", "#E2E7D4");
            medicineValue = Resource(resources, 2, "ЛЕКАРСТВА", "#DCE8E2");

            var goal = Panel("Current goal", safe, "#F8F4E9");
            Top(goal, 250, 133);
            Label(goal, "БЛИЖАЙШАЯ ЦЕЛЬ", 22, secondary, TextAnchor.UpperLeft, 24, 15, -48, 30);
            objective = Label(goal, "", 31, ink, TextAnchor.UpperLeft, 24, 47, -48, 42);
            objectiveHint = Label(goal, "", 23, secondary, TextAnchor.UpperLeft, 24, 91, -48, 30);

            var dock = Panel("Clinic actions", safe, "#F8F4E9");
            Bottom(dock, 16, 451);
            status = Label(dock, "", 26, secondary, TextAnchor.MiddleLeft, 24, 13, -48, 48);
            admit = ActionButton(dock, "Admit patient", "#4D8075", Color.white, out admitLabel);
            Top(admit.GetComponent<RectTransform>(), 77, 96, 20, 20);
            admit.onClick.AddListener(() => perform(simulation.Admit));
            var supplyRow = Rect("Supply actions", dock);
            Top(supplyRow, 187, 98, 20, 20);
            gather = ActionButton(supplyRow, "Gather herbs", "#E2E7D4", ink, out gatherLabel);
            craft = ActionButton(supplyRow, "Craft medicine", "#DCE8E2", ink, out craftLabel);
            Range(gather.GetComponent<RectTransform>(), 0, .5f);
            Range(craft.GetComponent<RectTransform>(), .5f, 1);
            gather.onClick.AddListener(() => perform(simulation.Gather));
            craft.onClick.AddListener(() => perform(simulation.Craft));
            var upgradeRow = Rect("Expansion actions", dock);
            Top(upgradeRow, 299, 86, 20, 20);
            buyBed = ActionButton(upgradeRow, "Add bed", "#ECE6D9", ink, out bedLabel);
            upgrade = ActionButton(upgradeRow, "Upgrade doctor", "#ECE6D9", ink, out upgradeLabel);
            Range(buyBed.GetComponent<RectTransform>(), 0, .5f);
            Range(upgrade.GetComponent<RectTransform>(), .5f, 1);
            buyBed.onClick.AddListener(() => perform(simulation.BuyBed));
            upgrade.onClick.AddListener(() => perform(simulation.Upgrade));
            Label(dock, "На лечение одного пациента нужно 1 лекарство", 23, secondary, TextAnchor.MiddleCenter, 20, 397, -40, 35);

            var toastRect = Panel("Notification", safe, "#304B48");
            Bottom(toastRect, 487, 76);
            toast = toastRect.gameObject;
            toastLabel = Label(toastRect, "", 28, Color.white, TextAnchor.MiddleCenter, 18, 0, -36, 76);
            toast.SetActive(false);
            Render();
        }

        public void Render()
        {
            var state = simulation.State;
            coinValue.text = state.coins.ToString();
            herbValue.text = state.herbs.ToString();
            medicineValue.text = state.medicine.ToString();
            levelLabel.text = "Врач · " + state.level + " ур.";
            status.text = "В очереди: " + simulation.WaitingCount + "    Кровати: " + simulation.BusyBeds + "/" + state.bedCount + "    Вылечено: " + state.cured;
            admit.interactable = simulation.CanAdmit;
            admitLabel.text = simulation.WaitingCount == 0 ? "Ожидаем пациентов" : state.medicine == 0 ? "Сначала приготовьте лекарства" : simulation.BusyBeds >= state.bedCount ? "Все кровати заняты" : "Принять пациента   ·   1 лекарство";
            gather.interactable = state.gardenCooldown <= 0;
            gatherLabel.text = state.gardenCooldown > 0 ? "Травы растут\n" + Mathf.CeilToInt(state.gardenCooldown) + " сек." : "Собрать травы\n+4 травы";
            craft.interactable = state.herbs >= 3 && state.craftRemaining <= 0;
            craftLabel.text = state.craftRemaining > 0 ? "Готовим лекарства\n" + Mathf.CeilToInt(state.craftRemaining) + " сек." : "Приготовить лекарство\n3 травы → 2 лекарства";
            buyBed.interactable = state.bedCount < 4 && state.coins >= simulation.NextBedCost;
            bedLabel.text = state.bedCount >= 4 ? "Все 4 кровати открыты" : "Добавить кровать\n" + simulation.NextBedCost + " монет";
            upgrade.interactable = state.level < 3 && state.coins >= simulation.UpgradeCost;
            upgradeLabel.text = state.level >= 3 ? "Врач · максимум" : "Улучшить врача\n" + simulation.UpgradeCost + " монет";
            if (state.cured == 0)
            {
                objective.text = "Вылечите первого пациента";
                objectiveHint.text = simulation.BusyBeds > 0 ? "Врач уже помогает. Дождитесь окончания лечения" : "Нажмите «Принять пациента», чтобы начать";
            }
            else if (state.batchesCrafted == 0)
            {
                objective.text = "Приготовьте лекарства";
                objectiveHint.text = "Собирайте травы в саду и готовьте новые порции";
            }
            else if (state.bedCount < 3)
            {
                objective.text = "Расширьте палату до 3 кроватей";
                objectiveHint.text = "Лечение приносит монеты для развития лечебницы";
            }
            else if (state.level < 2)
            {
                objective.text = "Улучшите врача до 2 уровня";
                objectiveHint.text = "Лечение станет быстрее и принесёт больше монет";
            }
            else
            {
                objective.text = state.cured < 15 ? "Помогите 15 пациентам: " + state.cured + "/15" : "Лечебница успешно работает!";
                objectiveHint.text = state.cured < 15 ? "Поддерживайте запас лекарств и принимайте пациентов" : "Продолжайте лечить людей и развивать палату";
            }
            if (toast != null && Time.unscaledTime > toastUntil) toast.SetActive(false);
        }

        public void ShowMessage(string message)
        {
            toastLabel.text = message;
            toastUntil = Time.unscaledTime + 3.5f;
            toast.SetActive(true);
        }

        private Text Resource(RectTransform parent, int index, string title, string color)
        {
            var tile = Panel(title, parent, color);
            Range(tile, index / 3f, (index + 1) / 3f);
            Label(tile, title, 23, secondary, TextAnchor.UpperLeft, 20, 12, -40, 32);
            return Label(tile, "0", 42, ink, TextAnchor.UpperLeft, 20, 42, -40, 58);
        }

        private Button ActionButton(RectTransform parent, string name, string color, Color textColor, out Text label)
        {
            var rect = Panel(name, parent, color);
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(.8f, .85f, .8f);
            colors.disabledColor = new Color(.78f, .78f, .78f, .65f);
            button.colors = colors;
            button.targetGraphic = rect.GetComponent<Image>();
            button.targetGraphic.raycastTarget = true;
            label = Label(rect, "", 29, textColor, TextAnchor.MiddleCenter, 12, 0, -24, 0);
            Stretch(label.rectTransform, 12);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 23;
            label.resizeTextMaxSize = 30;
            return button;
        }

        private RectTransform Panel(string name, Transform parent, string color)
        {
            var rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = panelSprite;
            image.type = Image.Type.Sliced;
            image.color = ColorHex(color);
            image.raycastTarget = false;
            return rect;
        }

        private Text Label(RectTransform parent, string value, int size, Color color, TextAnchor alignment, float x, float y, float widthOffset, float height)
        {
            var rect = Rect("Text", parent);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(widthOffset, height);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.text = value;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void Top(RectTransform rect, float y, float height, float left = 32, float right = 32)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2((left - right) * .5f, -y);
            rect.sizeDelta = new Vector2(-left - right, height);
        }

        private static void Bottom(RectTransform rect, float y, float height)
        {
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(.5f, 0);
            rect.anchoredPosition = new Vector2(0, y);
            rect.sizeDelta = new Vector2(-64, height);
        }

        private static void Range(RectTransform rect, float left, float right)
        {
            rect.anchorMin = new Vector2(left, 0);
            rect.anchorMax = new Vector2(right, 1);
            rect.offsetMin = new Vector2(6, 0);
            rect.offsetMax = new Vector2(-6, 0);
        }

        private static void Stretch(RectTransform rect, float inset = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset;
            rect.offsetMax = Vector2.one * -inset;
        }

        private static Color ColorHex(string hex) { ColorUtility.TryParseHtmlString(hex, out var color); return color; }

        private void CreatePanelSprite()
        {
            const int side = 64;
            const float radius = 16;
            panelTexture = new Texture2D(side, side, TextureFormat.RGBA32, false);
            panelTexture.wrapMode = TextureWrapMode.Clamp;
            for (var y = 0; y < side; y++)
            for (var x = 0; x < side; x++)
            {
                var dx = Mathf.Max(radius - x - .5f, x + .5f - (side - radius), 0);
                var dy = Mathf.Max(radius - y - .5f, y + .5f - (side - radius), 0);
                var alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy));
                panelTexture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
            panelTexture.Apply();
            panelSprite = Sprite.Create(panelTexture, new Rect(0, 0, side, side), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect, Vector4.one * radius);
        }

        private void OnDestroy() { if (panelSprite != null) Destroy(panelSprite); if (panelTexture != null) Destroy(panelTexture); }
    }
}
