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

        private void Start() => ShowTitle();

        private void ShowOnly(GameObject visible)
        {
            titlePanel.SetActive(visible == titlePanel);
            helpPanel.SetActive(visible == helpPanel);
            setupPanel.SetActive(visible == setupPanel);
        }
    }
}
