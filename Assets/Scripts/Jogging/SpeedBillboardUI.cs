using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ResortSports.Jogging
{
    /// <summary>
    /// 월드 스페이스 캔버스에 현재 속도(km/h, m/s)를 표시하고, 항상 카메라를 향하는 빌보드 UI.
    /// TMP_Text가 없으면 런타임에서 자동으로 생성한다.
    /// </summary>
    [ExecuteAlways]
    public class SpeedBillboardUI : MonoBehaviour
    {
        public enum SpeedUnit { MetersPerSecond, KilometersPerHour, Both }

        [Header("References")]
        public JoggingPlayerController player;
        public TMP_Text speedText;
        public Text legacySpeedText;
        public Canvas targetCanvas;
        [Tooltip("바라볼 대상 (보통 메인 카메라). 비워두면 Camera.main 사용.")]
        public Transform lookTarget;
        public Transform followTarget;
        public Vector3 followLocalOffset = new Vector3(0f, -0.25f, 1.25f);

        [Header("Display")]
        public SpeedUnit unit = SpeedUnit.Both;
        [Tooltip("표시 갱신 평활(초)")]
        [Range(0f, 0.5f)] public float displaySmoothing = 0.1f;
        [Tooltip("Y축만 회전 (수평 빌보드)")]
        public bool lockYAxis = true;
        public bool visibleOnlyWhileRunning = true;

        private float _shownSpeed;

        private void Awake()
        {
            if (targetCanvas == null) targetCanvas = GetComponent<Canvas>();
        }

        private void LateUpdate()
        {
            UpdateVisibility();
            UpdateFollowPosition();
            UpdateBillboard();
            UpdateText();
        }

        private void UpdateVisibility()
        {
            bool visible = !visibleOnlyWhileRunning || (player != null && player.IsRunning);
            if (targetCanvas != null) targetCanvas.enabled = visible;
            if (speedText != null) speedText.enabled = visible;
            if (legacySpeedText != null) legacySpeedText.enabled = visible;
        }

        private void UpdateFollowPosition()
        {
            if (followTarget == null) return;
            transform.position = followTarget.TransformPoint(followLocalOffset);
        }

        private void UpdateBillboard()
        {
            Transform t = lookTarget != null
                ? lookTarget
                : (Camera.main != null ? Camera.main.transform : null);
            if (t == null) return;

            Vector3 dir = transform.position - t.position;
            if (lockYAxis) dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) return;
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        private void UpdateText()
        {
            if (player == null || (speedText == null && legacySpeedText == null)) return;

            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            float a = displaySmoothing <= 0f ? 1f : 1f - Mathf.Exp(-dt / displaySmoothing);
            _shownSpeed = Mathf.Lerp(_shownSpeed, player.CurrentSpeed, a);

            switch (unit)
            {
                case SpeedUnit.MetersPerSecond:
                    SetText($"{_shownSpeed:0.0} m/s");
                    break;
                case SpeedUnit.KilometersPerHour:
                    SetText($"{_shownSpeed * 3.6f:0.0} km/h");
                    break;
                case SpeedUnit.Both:
                    SetText($"{_shownSpeed * 3.6f:0.0} km/h\n{_shownSpeed:0.00} m/s");
                    break;
            }
        }

        private void SetText(string text)
        {
            if (speedText != null) speedText.text = text;
            if (legacySpeedText != null) legacySpeedText.text = text;
        }
    }
}
