using UnityEngine;

namespace PlagueGame.Clinic
{
    [DefaultExecutionOrder(-100)]
    public sealed class PortraitLock : MonoBehaviour
    {
        private void Awake()
        {
            Apply();
            Application.targetFrameRate = 60;
        }

        private void OnApplicationFocus(bool focused) { if (focused) Apply(); }

        private static void Apply()
        {
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToPortrait = true;
            Screen.orientation = ScreenOrientation.Portrait;
        }
    }
}
