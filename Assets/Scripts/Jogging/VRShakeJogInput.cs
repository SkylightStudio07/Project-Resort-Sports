using UnityEngine;

namespace ResortSports.Jogging
{
    /// <summary>
    /// 양손 컨트롤러의 움직임(상하 흔들기)을 측정해 정규화된 흔들기 강도를 산출한다.
    /// XRI 3.x의 Controller 게임오브젝트(=TrackedPoseDriver가 붙은 Transform)를 그대로 참조하면 된다.
    /// </summary>
    public class VRShakeJogInput : MonoBehaviour
    {
        [Header("Hand References")]
        [Tooltip("왼손 컨트롤러 트랜스폼 (예: XR Origin / Camera Offset / LeftHand Controller)")]
        public Transform leftHand;
        [Tooltip("오른손 컨트롤러 트랜스폼")]
        public Transform rightHand;
        [Tooltip("기준이 되는 헤드(카메라). 머리 기준 로컬 속도로 변환해 머리 회전과 무관하게 측정.")]
        public Transform head;

        [Header("Tuning")]
        [Tooltip("측정에 사용하는 평활 시간(초). 짧을수록 즉각적, 길수록 부드러움.")]
        [Range(0.05f, 1f)] public float smoothing = 0.2f;

        [Tooltip("이 속도(m/s) 이상이면 강도 1로 정규화")]
        [Range(0.5f, 6f)] public float maxHandSpeed = 2.5f;

        [Tooltip("이 미만의 미세 진동은 무시 (m/s)")]
        [Range(0f, 1f)] public float deadZone = 0.15f;

        [Tooltip("양손 모두 흔들 때 가산 보너스 (0~1)")]
        [Range(0f, 1f)] public float bothHandsBonus = 0.25f;

        [Header("Footstep SFX")]
        public JoggingPlayerController player;
        public AudioSource footstepAudioSource;
        public AudioClip[] footstepClips;
        [Range(0f, 1f)] public float footstepIntensityThreshold = 0.25f;
        [Range(0.05f, 1f)] public float footstepMinInterval = 0.28f;
        [Range(0f, 1f)] public float footstepVolume = 0.8f;

        private Vector3 _prevLeftLocal, _prevRightLocal;
        private float _smoothedIntensity;
        private bool _initialized;
        private float _nextFootstepTime;

        /// <summary>현재 정규화 흔들기 강도 (0~1+).</summary>
        public float Intensity => _smoothedIntensity;

        private void LateUpdate()
        {
            if (leftHand == null || rightHand == null) return;

            // 머리 기준 로컬 좌표(머리가 없으면 월드)
            Vector3 lLocal = head != null ? head.InverseTransformPoint(leftHand.position) : leftHand.position;
            Vector3 rLocal = head != null ? head.InverseTransformPoint(rightHand.position) : rightHand.position;

            if (!_initialized)
            {
                _prevLeftLocal = lLocal;
                _prevRightLocal = rLocal;
                _initialized = true;
                return;
            }

            float dt = Mathf.Max(Time.deltaTime, 1e-4f);

            // 수직 성분(상하 펌프 동작)을 우선시
            float lSpeed = Mathf.Abs((lLocal.y - _prevLeftLocal.y)) / dt;
            float rSpeed = Mathf.Abs((rLocal.y - _prevRightLocal.y)) / dt;

            lSpeed = Mathf.Max(0f, lSpeed - deadZone);
            rSpeed = Mathf.Max(0f, rSpeed - deadZone);

            float lN = Mathf.Clamp01(lSpeed / maxHandSpeed);
            float rN = Mathf.Clamp01(rSpeed / maxHandSpeed);

            float combined = Mathf.Max(lN, rN);
            if (lN > 0.1f && rN > 0.1f)
                combined = Mathf.Clamp01(combined + bothHandsBonus * Mathf.Min(lN, rN));

            // 1차 저역통과
            float a = 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, smoothing));
            _smoothedIntensity = Mathf.Lerp(_smoothedIntensity, combined, a);
            TryPlayFootstepSfx();

            _prevLeftLocal = lLocal;
            _prevRightLocal = rLocal;
        }

        private void TryPlayFootstepSfx()
        {
            if (player != null && !player.IsRunning) return;
            if (footstepAudioSource == null || footstepClips == null || footstepClips.Length == 0) return;

            if (_smoothedIntensity < footstepIntensityThreshold || Time.time < _nextFootstepTime) return;

            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            if (clip == null) return;

            footstepAudioSource.PlayOneShot(clip, footstepVolume);
            _nextFootstepTime = Time.time + footstepMinInterval;
        }
    }
}
