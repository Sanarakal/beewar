using UnityEngine;
using UnityEngine.UI;

namespace PlagueGame.Kingdom
{
    public sealed partial class KingdomHud
    {
        public ResearchKind SelectedResearch { get; private set; }
        private GameObject academy;
        private readonly Button[] researchButtons = new Button[KingdomState.ResearchCount];
        private Text academySummary, researchTitle, researchDescription, researchEffect, researchCost, researchQueue, researchHint;
        private Image researchProgress;
        private Button researchStart;

        private void BuildAcademy()
        {
            var overlay = Panel("Academy overlay", transform, "#283E32"); Stretch(overlay);
            overlay.GetComponent<Image>().color = new Color(.08f, .15f, .11f, .94f);
            overlay.GetComponent<Image>().raycastTarget = true;
            academy = overlay.gameObject;
            var card = Panel("Academy card", overlay, "#F6EDDA");
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(.5f, .5f);
            card.sizeDelta = new Vector2(960, 1500);
            Label(card, "ЗНАНИЯ ПЕРВОУЛИЯ", 22, Muted, 36, 32, -72, 40);
            Label(card, "Академия Цветения", 43, Ink, 36, 91, -72, 65);
            academySummary = Label(card, "", 25, Muted, 36, 165, -72, 64);
            for (var i = 0; i < researchButtons.Length; i++)
            {
                var kind = (ResearchKind)i;
                var button = Button(card, "Research " + kind, "#E9E2CB", Ink, 27);
                Top(button.GetComponent<RectTransform>(), 240 + i * 122, 108, 32, 32);
                button.onClick.AddListener(() => { SelectedResearch = kind; RenderAcademy(); });
                researchButtons[i] = button;
            }
            var detail = Panel("Technology details", card, "#E8E4D2"); Top(detail, 752, 315, 32, 32);
            researchTitle = Label(detail, "", 32, Ink, 22, 16, -44, 53);
            researchDescription = Label(detail, "", 25, Muted, 22, 79, -44, 99);
            researchEffect = Label(detail, "", 27, Accent, 22, 189, -44, 45);
            researchCost = Label(detail, "", 24, Ink, 22, 245, -44, 62);
            researchQueue = Label(card, "", 25, Ink, 36, 1087, -72, 68);
            researchProgress = Progress(card, "Research progress", 1157);
            researchStart = Button(card, "Start research", "#386751", Color.white, 30);
            Top(researchStart.GetComponent<RectTransform>(), 1190, 90, 32, 32);
            researchStart.onClick.AddListener(() => perform(() => simulation.StartResearch(SelectedResearch)));
            researchHint = Label(card, "", 24, Muted, 36, 1294, -72, 78);
            var close = Button(card, "Close academy", "#E9E2CB", Ink, 29);
            Top(close.GetComponent<RectTransform>(), 1390, 78, 32, 32);
            SetText(close, "Вернуться в улей"); close.onClick.AddListener(() => academy.SetActive(false));
            academy.SetActive(false);
        }

        public void ShowAcademy()
        {
            SelectBuilding(BuildingKind.Academy);
            academy.SetActive(true); academy.transform.SetAsLastSibling(); RenderAcademy();
        }

        private void RenderAcademy()
        {
            if (academy == null) return;
            var order = simulation.State.research;
            academySummary.text = "Академия · ур. " + simulation.Level(BuildingKind.Academy) + "   |   Изучено: " + simulation.ResearchTotal + "/20\nОдна очередь исследований · до 5 уровней технологии";
            for (var i = 0; i < researchButtons.Length; i++)
            {
                var kind = (ResearchKind)i; var level = simulation.ResearchLevel(kind);
                var studying = order != null && order.kind == kind;
                SetText(researchButtons[i], KingdomSimulation.ResearchName(kind) + " · " + level + "/5" + (studying ? " · ИЗУЧАЕТСЯ" : "") + "\n" + KingdomSimulation.ResearchEffect(kind, level));
                Tint(researchButtons[i], kind == SelectedResearch);
            }
            var selectedLevel = simulation.ResearchLevel(SelectedResearch);
            var nextLevel = Mathf.Min(5, selectedLevel + 1);
            researchTitle.text = KingdomSimulation.ResearchName(SelectedResearch) + " · " + selectedLevel + "/5";
            researchDescription.text = KingdomSimulation.ResearchDescription(SelectedResearch);
            researchEffect.text = selectedLevel == 5 ? KingdomSimulation.ResearchEffect(SelectedResearch, 5) + " · максимум" :
                KingdomSimulation.ResearchEffect(SelectedResearch, selectedLevel) + " → +" + KingdomSimulation.ResearchPercent(SelectedResearch, nextLevel) + "%";
            researchCost.text = selectedLevel == 5 ? "Все уровни этой технологии открыты" : simulation.ResearchCost(SelectedResearch).Text + "\nВремя: " + Seconds(simulation.ResearchDuration(SelectedResearch));
            researchQueue.text = order == null ? "Учёные готовы к новому открытию" :
                KingdomSimulation.ResearchName(order.kind) + " · уровень " + order.targetLevel + "\nДо открытия: " + Left(order.endsAt);
            Fill(researchProgress, order == null ? 0 : Fraction(order.startedAt, order.endsAt));
            var block = simulation.ResearchBlock(SelectedResearch);
            researchStart.interactable = block.Length == 0;
            SetText(researchStart, order != null ? "Исследование · " + Left(order.endsAt) : selectedLevel == 5 ? "Технология изучена" : "Изучить · уровень " + nextLevel);
            researchHint.text = block.Length > 0 ? block : "Строительство и обучение идут параллельно.\nИсследование завершится и после выхода из игры.";
        }
    }
}
