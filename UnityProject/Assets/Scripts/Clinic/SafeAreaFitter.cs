using UnityEngine;

namespace PlagueGame.Clinic
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect lastArea;
        private Vector2Int lastScreen;

        private void Update()
        {
            var area = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (area == lastArea && size == lastScreen) return;
            if (size.x <= 0 || size.y <= 0) return;
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(area.xMin / size.x, area.yMin / size.y);
            rect.anchorMax = new Vector2(area.xMax / size.x, area.yMax / size.y);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            lastArea = area;
            lastScreen = size;
        }
    }
}
