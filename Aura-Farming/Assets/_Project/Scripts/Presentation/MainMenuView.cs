using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AuraFarming.Presentation
{
    public sealed class MainMenuView : MonoBehaviour
    {
        private const string PlayerCountKey = "AuraFarming.PlayerCount";

        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject helpPanel;
        [SerializeField] private GameObject setupPanel;
        [SerializeField] private Dropdown playerCountDropdown;
        [SerializeField] private Font brandingFont;
        [SerializeField] private Font uiFont;
        [SerializeField] private Font uiMediumFont;

        public void Configure(GameObject title, GameObject help, GameObject setup, Dropdown playerDropdown)
        {
            titlePanel = title;
            helpPanel = help;
            setupPanel = setup;
            playerCountDropdown = playerDropdown;
        }

        public void ShowTitle() => ShowOnly(titlePanel);
        public void ShowHelp() => ShowOnly(helpPanel);
        public void ShowSetup() => ShowOnly(setupPanel);

        public void StartGame()
        {
            PlayerPrefs.SetInt(PlayerCountKey, playerCountDropdown.value + 2);
            SceneManager.LoadScene("Game");
        }

        private void Start()
        {
            ApplyPremiumStyle();
            ShowTitle();
        }

        private void ApplyPremiumStyle()
        {
            var canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null) return;
            var background = canvas.GetComponent<Image>() ?? canvas.gameObject.AddComponent<Image>();
            background.color = new Color(0.012f, 0.008f, 0.02f, 1f);
            background.raycastTarget = false;
            CreateMenuAccent(canvas.transform, new Vector2(.78f, .52f), new Vector2(.52f, 1.25f), 18f, new Color(.20f, .01f, .13f, .72f));
            CreateMenuAccent(canvas.transform, new Vector2(.82f, .52f), new Vector2(.025f, .82f), -18f, new Color(1f, .04f, .42f, .72f));
            CreateMenuAccent(canvas.transform, new Vector2(.68f, .19f), new Vector2(.32f, .012f), 0f, new Color(1f, .04f, .42f, .5f));
            foreach (var text in canvas.GetComponentsInChildren<Text>(true))
            {
                var isTitle = text.name.ToLowerInvariant().Contains("title") || text.text.ToUpperInvariant().Contains("AURA") || text.text.ToUpperInvariant().Contains("TABLE IS");
                var selectedFont = isTitle ? brandingFont : uiMediumFont;
                if (selectedFont != null) text.font = selectedFont;
                text.color = isTitle ? Color.white : new Color(0.78f, 0.74f, 0.82f);
            }
            foreach (var button in canvas.GetComponentsInChildren<Button>(true))
            {
                var primary = button.GetComponentInChildren<Text>(true)?.text.ToUpperInvariant().Contains("PLAY") == true;
                var rect = button.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(primary ? 500f : 460f, primary ? 90f : 76f);
                foreach (var label in button.GetComponentsInChildren<Text>(true))
                {
                    if (uiFont != null) label.font = uiFont;
                    label.fontStyle = FontStyle.Normal;
                    label.alignment = TextAnchor.MiddleCenter;
                    label.rectTransform.offsetMin = new Vector2(20f, 0f);
                    label.rectTransform.offsetMax = new Vector2(-20f, 0f);
                }
                var image = button.GetComponent<Image>();
                if (image != null) image.color = primary ? new Color(0.83f, 0.04f, 0.48f, 1f) : new Color(0.025f, 0.015f, 0.035f, 1f);
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 0.18f, 0.62f);
                colors.pressedColor = new Color(0.55f, 0.02f, 0.28f);
                button.colors = colors;
                var outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 0.12f, 0.58f, primary ? 0.8f : 0.65f);
                outline.effectDistance = new Vector2(primary ? 3f : 1.5f, primary ? -3f : -1.5f);
                CreateButtonCut(button.transform, primary);
            }
        }

        private static void CreateButtonCut(Transform button, bool primary)
        {
            var cut = new GameObject("Angular Accent", typeof(RectTransform), typeof(Image));
            cut.transform.SetParent(button, false);
            var rect = cut.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f); rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, .5f); rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(primary ? 34f : 18f, primary ? 132f : 108f);
            rect.localRotation = Quaternion.Euler(0f, 0f, 18f);
            var image = cut.GetComponent<Image>(); image.color = primary ? new Color(.12f, .01f, .08f, .9f) : new Color(1f, .08f, .45f, .85f); image.raycastTarget = false;
        }

        private static void CreateMenuAccent(Transform parent, Vector2 anchor, Vector2 size, float rotation, Color color)
        {
            var accent = new GameObject("Neon Menu Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(parent, false);
            var rect = accent.GetComponent<RectTransform>();
            rect.anchorMin = anchor; rect.anchorMax = anchor; rect.sizeDelta = new Vector2(size.x * 1000f, size.y * 700f);
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var image = accent.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            accent.transform.SetAsLastSibling();
        }

        private void ShowOnly(GameObject visible)
        {
            titlePanel.SetActive(visible == titlePanel);
            helpPanel.SetActive(visible == helpPanel);
            setupPanel.SetActive(visible == setupPanel);
        }
    }
}
