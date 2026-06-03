using UnityEngine;
using UnityEngine.UI;

namespace ResortSports.Jogging
{
    public class StartPromptBillboardUI : MonoBehaviour
    {
        [Header("References")]
        public JoggingGameManager gameManager;
        public Canvas targetCanvas;
        public Text legacyText;
        public Transform lookTarget;

        [Header("Billboard")]
        public bool lockYAxis = true;

        private void Awake()
        {
            if (targetCanvas == null) targetCanvas = GetComponent<Canvas>();
            if (legacyText == null) legacyText = GetComponentInChildren<Text>();
        }

        private void LateUpdate()
        {
            UpdateVisibility();
            UpdateBillboard();
        }

        private void UpdateVisibility()
        {
            bool visible = gameManager != null
                && gameManager.State != JoggingGameManager.GameState.Running
                && gameManager.IsPlayerNearStart();

            if (targetCanvas != null) targetCanvas.enabled = visible;
            if (legacyText != null) legacyText.enabled = visible;
        }

        private void UpdateBillboard()
        {
            Transform target = lookTarget != null
                ? lookTarget
                : (Camera.main != null ? Camera.main.transform : null);

            if (target == null) return;

            Vector3 dir = transform.position - target.position;
            if (lockYAxis) dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) return;

            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }
}
