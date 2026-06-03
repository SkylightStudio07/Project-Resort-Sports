using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ResortSports.Jogging
{
    /// <summary>
    /// Jet ski entry prompt UI.
    /// Shows/hides a prompt panel and loads the jet ski scene when the YES button is clicked.
    ///
    /// Unity setup:
    /// 1. Place a World Space Canvas near the jet ski entry point and keep the prompt panel inactive by default.
    /// 2. Add a YES button and connect it to this script.
    /// 3. Call ShowPrompt/HidePrompt from a trigger script such as ExitDoorTrigger.
    /// </summary>
    public class JetSkiEntryPromptUI : MonoBehaviour
    {
        [Header("UI Components")]
        [Tooltip("Prompt panel. It is hidden on Start.")]
        public GameObject promptPanel;

        [Tooltip("YES button that starts the jet ski scene.")]
        public Button yesButton;

        [Header("Scene")]
        [Tooltip("Jet ski game scene name registered in Build Settings.")]
        public string jetSkiSceneName = "Jetski Draft GameScene Day";

        private void Start()
        {
            if (promptPanel != null)
                promptPanel.SetActive(false);

            if (yesButton != null)
                yesButton.onClick.AddListener(OnYesClicked);
        }

        public void ShowPrompt()
        {
            if (promptPanel != null)
                promptPanel.SetActive(true);
        }

        public void HidePrompt()
        {
            if (promptPanel != null)
                promptPanel.SetActive(false);
        }

        private void OnYesClicked()
        {
            HidePrompt();
            LoadJetSkiScene();
        }

        private void LoadJetSkiScene()
        {
            if (string.IsNullOrWhiteSpace(jetSkiSceneName))
            {
                Debug.LogWarning("[JetSkiEntryPromptUI] Jet ski scene name is empty.");
                return;
            }

            Debug.Log($"[JetSkiEntryPromptUI] Loading scene: {jetSkiSceneName}");
            SceneManager.LoadScene(jetSkiSceneName);
        }
    }
}
