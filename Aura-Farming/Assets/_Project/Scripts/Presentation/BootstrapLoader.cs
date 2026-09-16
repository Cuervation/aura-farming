using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AuraFarming.Presentation
{
    public sealed class BootstrapLoader : MonoBehaviour
    {
        private void Start()
        {
            var canvas = new GameObject("Bootstrap UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(canvas.transform, false);
            background.GetComponent<Image>().color = new Color(0.012f, 0.008f, 0.02f, 1f);
            var rect = background.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            var label = new GameObject("Aura Farming", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(canvas.transform, false);
            var text = label.GetComponent<Text>(); text.text = "AURA FARMING\n<color=#D10A70>INITIALIZING AURA...</color>"; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = 54; text.alignment = TextAnchor.MiddleCenter; text.color = Color.white; text.supportRichText = true;
            var lr = label.GetComponent<RectTransform>(); lr.anchorMin = new Vector2(.1f,.4f); lr.anchorMax = new Vector2(.9f,.6f); lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero;
            SceneManager.LoadScene("MainMenu");
        }
    }
}
