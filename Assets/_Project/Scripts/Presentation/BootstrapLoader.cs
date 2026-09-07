using UnityEngine;
using UnityEngine.SceneManagement;

namespace AuraFarming.Presentation
{
    public sealed class BootstrapLoader : MonoBehaviour
    {
        private void Start()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
