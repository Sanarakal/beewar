using System;
using PlagueGame.Clinic;
using UnityEngine;
using UnityEngine.UI;

namespace PlagueGame.Kingdom
{
    public sealed class KingdomHud : MonoBehaviour
    {
        public KingdomTab Tab { get; private set; }
        public BuildingKind SelectedBuilding { get; private set; }
        public int SelectedMonster { get; private set; }
        public bool BlocksWorldInput => (story != null && story.activeSelf) || (report != null && report.activeSelf);
        private int trainingTier = 1, requestedSquad = 10;
        private KingdomSimulation simulation;
        private KingdomWorldView world;
        private Font font;
        private Action<Func<KingdomAction>> perform;
        private Action<KingdomTab> changeTab;
        private RectTransform safe;
        private readonly GameObject[] screens = new GameObject[3];
        private readonly Button[] navigation = new Button[3], buildingButtons = new Button[6], monsterButtons = new Button[3], tierButtons = new Button[2], squadButtons = new Button[3];
        private readonly Button[] worldLabels = new Button[9];
        private Text food, wood, gold, title, statistics, objective;
        private Text buildingTitle, buildingDescription, buildingInfo, buildHint, armyDescription, armyInfo, armyCost, armyHint;
        private Text monsterTitle, monsterDescription, monsterInfo, toastText, reportText, reportTitle;
        private Button upgrade, train, attack, reportButton;
        private Image buildingProgress, trainingProgress, marchProgress;
        private GameObject toast, report, story, heroCaption;
        private Text storyTitle, storyText;
        private int observedStoryStage = -1;
        private float toastUntil;
        private Sprite rounded;
        private Texture2D panelTexture;
        private static readonly Color Ink = Hex("#283E32"), Muted = Hex("#796F58"), Accent = Hex("#386751"), Cream = Hex("#F6EDDA");

        public void Initialize(KingdomSimulation rules, Font uiFont, KingdomWorldView worldView, Action<Func<KingdomAction>> command, Action<KingdomTab> onTab)
        {
            simulation = rules; font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            world = worldView; perform = command; changeTab = onTab;
            MakeRoundedSprite();
            gameObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 2400); scaler.matchWidthOrHeight = 0;
            gameObject.AddComponent<GraphicRaycaster>();
            safe = Rect("Safe area", transform); Stretch(safe); safe.gameObject.AddComponent<SafeAreaFitter>();

            var heading = Rect("Heading", safe); Top(heading, 15, 85);
            Label(heading, "ЯНТАРНЫЙ ПРЕСТОЛ", 21, Muted, 0, 0, -200, 30);
            title = Label(heading, "Улей среди корней", 43, Ink, 0, 30, -120, 56);
            var version = Label(heading, "0.3 · УЛЕЙ", 20, Muted, 0, 0, 0, 35, TextAnchor.MiddleRight);
            version.rectTransform.anchorMin = new Vector2(.72f, 1);
            var resources = Rect("Resources", safe); Top(resources, 112, 91);
            food = Resource(resources, 0, "НЕКТАР", "#DFE6BF"); wood = Resource(resources, 1, "ВОСК", "#EFE1BD"); gold = Resource(resources, 2, "ЯНТАРЬ", "#EDD39B");
            statistics = Label(safe, "", 25, Ink, 32, 214, -64, 42);
            var goal = Panel("Next objective", safe, "#F6F0DF"); Top(goal, 269, 82);
            Label(goal, "ГЛАВА 1 · НОВЫЙ ДОМ", 18, Muted, 20, 8, -280, 26);
            var journal = Button(goal, "Open story", "#E8D4A4", Ink, 19); Top(journal.GetComponent<RectTransform>(), 5, 43, 800, 15);
            SetText(journal, "История"); journal.onClick.AddListener(ShowStory);
            objective = Label(goal, "", 27, Ink, 20, 34, -40, 41);

            var worldLayer = Rect("World labels", safe); Stretch(worldLayer);
            for (var i = 0; i < 9; i++)
            {
                var index = i;
                var button = Button(worldLayer, "World target " + i, "#F6EDDA", Ink, 21);
                var rect = button.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(i == 0 ? 220 : 208, 65);
                button.onClick.AddListener(() => { if (index < 6) SelectBuilding((BuildingKind)index); else SelectMonster(index - 6); });
                worldLabels[i] = button;
            }

            var dock = Panel("Actions dock", safe, "#F6EDDA"); Bottom(dock, 128, 602);
            var city = Rect("Settlement actions", dock); Stretch(city); screens[0] = city.gameObject;
            buildingTitle = Label(city, "", 35, Ink, 24, 18, -48, 56);
            buildingDescription = Label(city, "", 25, Muted, 24, 82, -48, 72);
            buildingInfo = Label(city, "", 26, Ink, 24, 158, -48, 70);
            buildingProgress = Progress(city, "Building progress", 239);
            upgrade = Button(city, "Upgrade building", "#386751", Color.white, 29); Top(upgrade.GetComponent<RectTransform>(), 270, 86, 22, 22);
            upgrade.onClick.AddListener(() => perform(() => simulation.StartUpgrade(SelectedBuilding)));
            buildHint = Label(city, "", 23, Muted, 24, 365, -48, 40);
            var buildingGrid = Rect("Building selection", city); Top(buildingGrid, 418, 157, 18, 18);
            for (var i = 0; i < 6; i++)
            {
                var kind = (BuildingKind)i; var button = Button(buildingGrid, "Select " + kind, "#E9E2CB", Ink, 23);
                Grid(button.GetComponent<RectTransform>(), i % 3, i / 3, 3, 2);
                button.onClick.AddListener(() => SelectBuilding(kind)); buildingButtons[i] = button;
            }

            var army = Rect("Army actions", dock); Stretch(army); screens[1] = army.gameObject;
            Label(army, "Стража Жала", 39, Ink, 24, 18, -48, 56);
            armyDescription = Label(army, "", 26, Ink, 24, 84, -48, 76);
            armyInfo = Label(army, "", 25, Muted, 24, 161, -48, 68);
            trainingProgress = Progress(army, "Training progress", 237);
            var tierRow = Rect("Troop tiers", army); Top(tierRow, 268, 67, 18, 18);
            for (var i = 0; i < 2; i++)
            {
                var tier = i + 1; var button = Button(tierRow, "Train tier " + tier, "#E9E2CB", Ink, 26);
                Grid(button.GetComponent<RectTransform>(), i, 0, 2, 1); button.onClick.AddListener(() => { trainingTier = tier; Render(); }); tierButtons[i] = button;
            }
            train = Button(army, "Train soldiers", "#386751", Color.white, 29); Top(train.GetComponent<RectTransform>(), 350, 90, 22, 22);
            train.onClick.AddListener(() => perform(() => simulation.StartTraining(trainingTier, 10)));
            armyCost = Label(army, "", 25, Ink, 24, 453, -48, 45);
            armyHint = Label(army, "", 23, Muted, 24, 509, -48, 72);

            var map = Rect("Map actions", dock); Stretch(map); screens[2] = map.gameObject;
            monsterTitle = Label(map, "", 36, Ink, 24, 16, -48, 55);
            monsterDescription = Label(map, "", 24, Muted, 24, 78, -48, 69);
            monsterInfo = Label(map, "", 25, Ink, 24, 154, -48, 81);
            marchProgress = Progress(map, "March progress", 239);
            var squadRow = Rect("Squad size", map); Top(squadRow, 268, 61, 18, 18);
            for (var i = 0; i < 3; i++)
            {
                var amount = i == 0 ? 10 : i == 1 ? 20 : 1000;
                var button = Button(squadRow, "Squad " + amount, "#E9E2CB", Ink, 25);
                Grid(button.GetComponent<RectTransform>(), i, 0, 3, 1);
                button.onClick.AddListener(() => { requestedSquad = amount; Render(); }); squadButtons[i] = button;
            }
            attack = Button(map, "Attack monster", "#386751", Color.white, 29); Top(attack.GetComponent<RectTransform>(), 343, 88, 22, 22);
            attack.onClick.AddListener(() => perform(() => simulation.SendMarch(SelectedMonster, requestedSquad)));
            var monsterRow = Rect("Monster selection", map); Top(monsterRow, 448, 71, 18, 18);
            for (var i = 0; i < 3; i++)
            {
                var index = i; var button = Button(monsterRow, "Select monster " + i, "#E9E2CB", Ink, 23);
                Grid(button.GetComponent<RectTransform>(), i, 0, 3, 1); button.onClick.AddListener(() => SelectMonster(index)); monsterButtons[i] = button;
            }
            reportButton = Button(map, "Battle report", "#ECDDB6", Ink, 25); Top(reportButton.GetComponent<RectTransform>(), 536, 47, 22, 22);
            reportButton.onClick.AddListener(ShowReport);

            var nav = Panel("Navigation", safe, "#283E32"); Bottom(nav, 10, 99);
            var navNames = new[] { "Улей", "Армия", "Карта" };
            for (var i = 0; i < 3; i++)
            {
                var tab = (KingdomTab)i; var button = Button(nav, "Tab " + tab, "#283E32", Color.white, 28);
                Grid(button.GetComponent<RectTransform>(), i, 0, 3, 1, 9);
                button.GetComponentInChildren<Text>().text = navNames[i]; button.onClick.AddListener(() => SetTab(tab)); navigation[i] = button;
            }
            var toastRect = Panel("Notification", safe, "#283E32"); Bottom(toastRect, 743, 67); toast = toastRect.gameObject;
            toastText = Label(toastRect, "", 25, Color.white, 18, 0, -36, 67, TextAnchor.MiddleCenter); toast.SetActive(false);
            BuildReport(); BuildStory();
            var caption = Rect("Hero caption", worldLayer); Top(caption, 395, 145); heroCaption = caption.gameObject;
            Label(caption, "РУТА", 48, Hex("#F2E4BE"), 14, 8, -28, 65);
            Label(caption, "Проводница · командир стражи", 25, Hex("#DFCC9E"), 14, 78, -28, 42);
            SetTab(KingdomTab.Settlement);
            if (!PlayerPrefs.HasKey("HiveStorySeen")) ShowStory();
        }

        public void SetTab(KingdomTab tab)
        {
            Tab = tab;
            for (var i = 0; i < 3; i++) { screens[i].SetActive(i == (int)tab); Tint(navigation[i], i == (int)tab, true); }
            changeTab(tab); Render();
        }
        public void SelectBuilding(BuildingKind kind) { SelectedBuilding = kind; if (Tab != KingdomTab.Settlement) SetTab(KingdomTab.Settlement); else Render(); }
        public void SelectMonster(int index) { SelectedMonster = index; if (Tab != KingdomTab.Map) SetTab(KingdomTab.Map); else Render(); }

        public void Render()
        {
            var state = simulation.State;
            food.text = ResourceText(state.food); wood.text = ResourceText(state.wood); gold.text = ResourceText(state.gold);
            title.text = Tab == KingdomTab.Map ? "Тропы Цветолесья" : Tab == KingdomTab.Army ? "Рута и стража улья" : "Улей среди корней";
            statistics.text = "Мощь  " + simulation.Power + "     В улье  " + simulation.AvailableTroops + "     В походе  " +
                (state.march == null ? 0 : state.march.phase == MarchPhase.Outbound ? state.march.tier1 + state.march.tier2 : state.march.survivors1 + state.march.survivors2);
            objective.text = simulation.Objective;
            heroCaption?.SetActive(Tab == KingdomTab.Army);
            if (observedStoryStage >= 0 && observedStoryStage != simulation.StoryStage) ShowMessage("Новая запись: «" + simulation.StoryTitle + "». Открой историю.");
            observedStoryStage = simulation.StoryStage;
            RenderBuilding(); RenderArmy(); RenderMap();
            for (var i = 0; i < 6; i++)
            {
                var name = new[] { "Цитадель", "Нектарный сад", "Мастерская", "Хранилище", "Казармы Жала", "Лагерь Крыльев" }[i];
                SetText(buildingButtons[i], name + " · " + simulation.Level((BuildingKind)i)); Tint(buildingButtons[i], i == (int)SelectedBuilding);
                SetText(worldLabels[i], name + "\nУровень " + simulation.Level((BuildingKind)i));
            }
            for (var i = 0; i < 3; i++)
            {
                var name = i == 0 ? "Долгоносик" : i == 1 ? "Шипокрыл" : "Паук";
                SetText(monsterButtons[i], name + "\nУровень " + (i + 1)); Tint(monsterButtons[i], i == SelectedMonster);
                SetText(worldLabels[i + 6], name + " · ур. " + (i + 1) + "\n" + (state.monsterReadyAt[i] > state.lastUtc ? "Вернётся: " + Left(state.monsterReadyAt[i]) : "Выбрать цель"));
            }
            if (toast != null && Time.unscaledTime > toastUntil) toast.SetActive(false);
            if (report != null && report.activeSelf) UpdateReport();
        }

        private void RenderBuilding()
        {
            var state = simulation.State; var kind = SelectedBuilding; var order = state.upgrade;
            buildingTitle.text = KingdomSimulation.BuildingName(kind) + " · уровень " + simulation.Level(kind);
            buildingDescription.text = simulation.BuildingBenefit(kind);
            var block = simulation.UpgradeBlock(kind);
            var ownOrder = order != null && order.building == kind;
            buildingInfo.text = ownOrder ? "Улучшение до " + order.targetLevel + " уровня\nОсталось " + Left(order.endsAt) :
                simulation.Level(kind) >= 5 ? "Постройка полностью развита в этом прототипе" :
                "Следующий уровень: " + (simulation.Level(kind) + 1) + "  ·  " + Seconds(simulation.UpgradeDuration(kind)) + "\n" + simulation.UpgradeCost(kind).Text;
            Fill(buildingProgress, ownOrder ? Fraction(order.startedAt, order.endsAt) : 0);
            SetText(upgrade, ownOrder ? "Идёт строительство · " + Left(order.endsAt) : simulation.Level(kind) >= 5 ? "Максимальный уровень" : "Улучшить до " + (simulation.Level(kind) + 1) + " уровня");
            upgrade.interactable = block.Length == 0;
            buildHint.text = block.Length > 0 && !ownOrder ? block : "Ресурсы производятся автоматически, пока есть место";
            if (kind == BuildingKind.Barracks && block.Length == 0) buildHint.text = "Обучение солдат находится в разделе «Армия»";
        }

        private void RenderArmy()
        {
            var state = simulation.State; var order = state.training;
            armyDescription.text = "В улье: " + state.tier1 + " солдат I ступени, " + state.tier2 + " — II\nРута · ур. " + simulation.HeroLevel + "   |   Вместимость похода: " + simulation.ArmyCapacity;
            armyInfo.text = order == null ? "Казармы " + simulation.Level(BuildingKind.Barracks) + " уровня готовы к работе\nПартия из 10 солдат: " + Seconds(simulation.TrainingDuration(trainingTier, 10)) :
                "Обучаются " + order.count + " солдат " + (order.tier == 1 ? "I" : "II") + " ступени\nОсталось " + Left(order.endsAt);
            Fill(trainingProgress, order == null ? 0 : Fraction(order.startedAt, order.endsAt));
            for (var i = 0; i < 2; i++) { SetText(tierButtons[i], i == 0 ? "I ступень" : simulation.UnlockedTier >= 2 ? "II ступень" : "II · нужен лагерь 3"); tierButtons[i].interactable = i == 0 || simulation.UnlockedTier >= 2; Tint(tierButtons[i], trainingTier == i + 1); }
            var block = simulation.TrainingBlock(trainingTier, 10);
            train.interactable = block.Length == 0;
            SetText(train, order == null ? "Обучить 10 солдат · " + (trainingTier == 1 ? "I" : "II") + " ступень" : "Обучение · " + Left(order.endsAt));
            armyCost.text = simulation.TrainingCost(trainingTier, 10).Text;
            armyHint.text = block.Length > 0 && order == null ? block : "Лагерь увеличивает отряд. Казармы ускоряют обучение.\nВо время похода солдаты недоступны в улье.";
        }

        private void RenderMap()
        {
            var state = simulation.State; var monster = KingdomSimulation.Monsters[SelectedMonster];
            monsterTitle.text = monster.name + " · ур. " + monster.level;
            monsterDescription.text = monster.description;
            var count = simulation.SquadSize(requestedSquad);
            var block = simulation.MarchBlock(SelectedMonster, requestedSquad);
            if (state.march != null)
            {
                var order = state.march; var outbound = order.phase == MarchPhase.Outbound;
                monsterInfo.text = (outbound ? "Отряд идёт к цели: " : "Отряд возвращается: ") + KingdomSimulation.Monsters[order.monster].name + "\n" +
                    (outbound ? "До боя: " + Left(order.arrivesAt) : "До возвращения: " + Left(order.homeAt));
                Fill(marchProgress, Fraction(outbound ? order.startedAt : order.arrivesAt, outbound ? order.arrivesAt : order.homeAt));
            }
            else
            {
                var preview = simulation.Preview(SelectedMonster, requestedSquad);
                monsterInfo.text = "+" + monster.food + " нектара  ·  +" + monster.wood + " воска  ·  +" + monster.gold + " янтаря\n" +
                    (count == 0 ? "Обучите солдат, чтобы отправить отряд" : "Отряд: " + count + ". " + (preview.won ? "Победа; потери: " : "Риск поражения; потери: ") + (count - preview.survivors1 - preview.survivors2));
                Fill(marchProgress, 0);
                if (block.Length > 0 && state.monsterReadyAt[SelectedMonster] > state.lastUtc) monsterDescription.text = block;
            }
            for (var i = 0; i < 3; i++) { SetText(squadButtons[i], i == 0 ? "Отряд 10" : i == 1 ? "Отряд 20" : "Макс. " + Math.Min(simulation.ArmyCapacity, simulation.AvailableTroops)); Tint(squadButtons[i], requestedSquad == (i == 0 ? 10 : i == 1 ? 20 : 1000)); }
            attack.interactable = block.Length == 0;
            SetText(attack, state.march != null ? "Отряд в походе" : count == 0 ? "Сначала обучите солдат" : state.monsterReadyAt[SelectedMonster] > state.lastUtc ? "Ожидаем появления противника" : "Атаковать · " + count + " солдат · путь " + Seconds(monster.travelSeconds));
            reportButton.interactable = state.report != null;
            SetText(reportButton, state.report == null ? "Здесь появится отчёт о бое" : "Открыть последний отчёт о бое");
        }

        public void PositionWorldLabels()
        {
            if (safe == null) return;
            var view = Camera.main;
            for (var i = 0; i < worldLabels.Length; i++)
            {
                var relevant = i < 6 ? Tab == KingdomTab.Settlement : Tab == KingdomTab.Map;
                var screen = view.WorldToScreenPoint(world.LabelPosition(i < 6 ? i : i - 6, i >= 6));
                worldLabels[i].gameObject.SetActive(relevant && view.pixelRect.Contains(screen) && screen.z > 0);
                if (!relevant) continue;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(safe, screen, null, out var point);
                worldLabels[i].GetComponent<RectTransform>().anchoredPosition = point;
            }
        }

        public void ShowMessage(string message) { toastText.text = message; toastUntil = Time.unscaledTime + 4; toast.SetActive(true); }

        private void BuildReport()
        {
            var overlay = Panel("Report overlay", transform, "#283E32"); Stretch(overlay);
            var dim = overlay.GetComponent<Image>(); dim.color = new Color(.1f, .18f, .2f, .82f); dim.raycastTarget = true;
            report = overlay.gameObject;
            var card = Panel("Report card", overlay, "#F6EDDA"); card.anchorMin = card.anchorMax = card.pivot = new Vector2(.5f, .5f); card.sizeDelta = new Vector2(960, 1000);
            Label(card, "БОЕВОЙ ОТЧЁТ", 22, Muted, 35, 30, -70, 40);
            reportTitle = Label(card, "", 45, Ink, 35, 86, -70, 75);
            reportText = Label(card, "", 31, Ink, 35, 186, -70, 610);
            var close = Button(card, "Close report", "#386751", Color.white, 30); Top(close.GetComponent<RectTransform>(), 866, 90, 30, 30);
            SetText(close, "Закрыть отчёт"); close.onClick.AddListener(() => report.SetActive(false)); report.SetActive(false);
        }
        private void BuildStory()
        {
            var overlay = Panel("Story overlay", transform, "#283E32"); Stretch(overlay);
            overlay.GetComponent<Image>().color = new Color(.08f, .15f, .11f, .9f); overlay.GetComponent<Image>().raycastTarget = true;
            story = overlay.gameObject;
            var card = Panel("Story card", overlay, "#F6EDDA"); card.anchorMin = card.anchorMax = card.pivot = new Vector2(.5f, .5f); card.sizeDelta = new Vector2(960, 1120);
            Label(card, "ЛЕТОПИСЬ ЦВЕТОЛЕСЬЯ", 22, Muted, 38, 32, -76, 40);
            storyTitle = Label(card, "", 42, Ink, 38, 98, -76, 110);
            storyText = Label(card, "", 29, Ink, 38, 226, -76, 700);
            var close = Button(card, "Close story", "#386751", Color.white, 29); Top(close.GetComponent<RectTransform>(), 988, 86, 32, 32);
            SetText(close, "Продолжить"); close.onClick.AddListener(() => { story.SetActive(false); PlayerPrefs.SetInt("HiveStorySeen", 1); PlayerPrefs.Save(); });
            story.SetActive(false);
        }
        public void ShowStory()
        {
            storyTitle.text = simulation.StoryTitle; storyText.text = simulation.StoryText;
            story.SetActive(true); story.transform.SetAsLastSibling();
        }
        private void ShowReport() { if (simulation.State.report == null) return; UpdateReport(); report.SetActive(true); }
        private void UpdateReport()
        {
            var result = simulation.State.report; if (result == null) return;
            reportTitle.text = result.won ? "Победа!" : "Поражение";
            reportText.text = KingdomSimulation.Monsters[result.monster].name + "\n\nОтправлено: " + result.sent + "\nВыжило: " + result.survivors + "\nПотеряно: " + result.lost +
                "\n\nДобыча: " + result.food + " нектара, " + result.wood + " воска, " + result.gold + " янтаря\nОпыт Руты: +" + result.experience +
                "\n\n" + (result.delivered ? "Отряд вернулся. Награды зачислены." : "Отряд ещё в пути. Добыча поступит после возвращения.") +
                "\n\nПотери в этой версии окончательные. Ресурсы сверх вместимости склада не сохраняются.";
        }

        private string ResourceText(double value) => ((int)Math.Floor(value)).ToString() + " / " + simulation.StorageCapacity;
        private string Left(double until) => Seconds(Math.Max(0, Math.Ceiling(until - simulation.State.lastUtc)));
        private static string Seconds(double seconds) { var value = (int)Math.Ceiling(seconds); return value < 60 ? value + " сек." : value / 60 + " мин. " + value % 60 + " сек."; }
        private float Fraction(double start, double end) => Mathf.Clamp01((float)((simulation.State.lastUtc - start) / (end - start)));
        private static void Fill(Image image, float fraction) { image.rectTransform.anchorMax = new Vector2(Mathf.Max(.005f, fraction), 1); image.gameObject.SetActive(fraction > 0); }
        private static void SetText(Button button, string value) => button.GetComponentInChildren<Text>().text = value;
        private static void Tint(Button button, bool selected, bool dark = false)
        {
            button.targetGraphic.color = selected ? Accent : dark ? Ink : Hex("#E9E2CB");
            button.GetComponentInChildren<Text>().color = selected || dark ? Color.white : Ink;
        }

        private Text Resource(RectTransform parent, int index, string name, string color)
        {
            var tile = Panel(name, parent, color); Grid(tile, index, 0, 3, 1, 6);
            Label(tile, name, 20, Muted, 18, 8, -36, 29);
            return Label(tile, "", 32, Ink, 18, 38, -36, 47);
        }
        private Image Progress(RectTransform parent, string name, float y)
        {
            var background = Panel(name, parent, "#DDE3D3"); Top(background, y, 12, 24, 24);
            var fill = Panel("Fill", background, "#C0A568"); Stretch(fill); return fill.GetComponent<Image>();
        }
        private Button Button(RectTransform parent, string name, string color, Color textColor, int size)
        {
            var rect = Panel(name, parent, color); var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>(); button.targetGraphic.raycastTarget = true;
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = Color.white; colors.pressedColor = new Color(.8f, .86f, .8f); colors.disabledColor = new Color(.8f, .8f, .8f, .65f); button.colors = colors;
            var label = Label(rect, "", size, textColor, 0, 0, 0, 0, TextAnchor.MiddleCenter); Stretch(label.rectTransform, 8);
            label.resizeTextForBestFit = true; label.resizeTextMinSize = size - 3; label.resizeTextMaxSize = size; return button;
        }
        private RectTransform Panel(string name, Transform parent, string color)
        {
            var rect = Rect(name, parent); var image = rect.gameObject.AddComponent<Image>(); image.sprite = rounded; image.type = Image.Type.Sliced; image.color = Hex(color); image.raycastTarget = false; return rect;
        }
        private Text Label(RectTransform parent, string value, int size, Color color, float x, float y, float width, float height, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            var rect = Rect("Text", parent); rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
            var text = rect.gameObject.AddComponent<Text>(); text.font = font; text.fontSize = size; text.text = value; text.color = color; text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate; text.raycastTarget = false; return text;
        }
        private static RectTransform Rect(string name, Transform parent) { var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect; }
        private static void Stretch(RectTransform rect, float inset = 0) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.one * inset; rect.offsetMax = Vector2.one * -inset; }
        private static void Top(RectTransform rect, float y, float height, float left = 32, float right = 32)
        { rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2((left - right) * .5f, -y); rect.sizeDelta = new Vector2(-left - right, height); }
        private static void Bottom(RectTransform rect, float y, float height)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = new Vector2(1, 0); rect.pivot = new Vector2(.5f, 0); rect.anchoredPosition = new Vector2(0, y); rect.sizeDelta = new Vector2(-64, height); }
        private static void Grid(RectTransform rect, int x, int y, int columns, int rows, float gap = 5)
        { rect.anchorMin = new Vector2(x / (float)columns, 1 - (y + 1) / (float)rows); rect.anchorMax = new Vector2((x + 1) / (float)columns, 1 - y / (float)rows); rect.offsetMin = Vector2.one * gap; rect.offsetMax = Vector2.one * -gap; }
        private static Color Hex(string value) { ColorUtility.TryParseHtmlString(value, out var color); return color; }
        private void MakeRoundedSprite()
        {
            const int side = 64; const float radius = 14;
            panelTexture = new Texture2D(side, side, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (var y = 0; y < side; y++) for (var x = 0; x < side; x++)
            {
                var dx = Mathf.Max(radius - x, x - (side - 1 - radius)); var dy = Mathf.Max(radius - y, y - (side - 1 - radius));
                var distance = Mathf.Sqrt(Mathf.Max(0, dx) * Mathf.Max(0, dx) + Mathf.Max(0, dy) * Mathf.Max(0, dy));
                panelTexture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(radius + .5f - distance)));
            }
            panelTexture.Apply(); rounded = Sprite.Create(panelTexture, new Rect(0, 0, side, side), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
        }
        private void OnDestroy() { if (rounded != null) Destroy(rounded); if (panelTexture != null) Destroy(panelTexture); }
    }
}
