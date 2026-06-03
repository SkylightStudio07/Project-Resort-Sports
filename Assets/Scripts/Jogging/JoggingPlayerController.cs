using UnityEngine;

namespace ResortSports.Jogging
{
    /// <summary>
    /// 트랙을 따라 XR Origin을 이동시키는 컨트롤러.
    /// 흔들기 강도(0~1)를 받아 목표 속도로 변환하고, 관성/감속을 적용해 트랙 distance를 증가시킨다.
    /// 머리 회전은 그대로 두므로 사용자는 자유롭게 둘러볼 수 있다.
    /// </summary>
    public class JoggingPlayerController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("이동시킬 대상 (보통 XR Origin 루트)")]
        public Transform xrOrigin;
        public JogTrack track;
        public VRShakeJogInput input;

        [Header("Speed")]
        [Tooltip("입력 강도 1일 때 도달하는 최대 속도 (m/s)")]
        [Range(1f, 12f)] public float maxSpeed = 5f;

        [Tooltip("가속(목표에 도달하는 데 걸리는 시간 상수, 초)")]
        [Range(0.05f, 3f)] public float accelTime = 0.4f;

        [Tooltip("감속(입력이 멈췄을 때 시간 상수, 초)")]
        [Range(0.05f, 3f)] public float decelTime = 0.8f;

        [Tooltip("이동을 시작할 최소 강도 임계값")]
        [Range(0f, 1f)] public float startThreshold = 0.05f;

        [Header("Orientation")]
        [Tooltip("XR Origin의 forward를 트랙 진행 방향으로 자동 회전")]
        public bool faceTrackDirection = true;
        [Tooltip("XR Origin forward가 트랙 진행 방향과 반대로 보일 때 켭니다.")]
        public bool invertTrackDirection = false;
        [Tooltip("자동 회전 속도 (deg/s). 0이면 즉시.")]
        public float turnSpeed = 180f;

        [Header("State (read-only)")]
        [SerializeField] private float _distance;
        [SerializeField] private float _speed;
        [SerializeField] private bool _isRunning;

        public bool IsRunning => _isRunning;
        public Transform XrOrigin => xrOrigin;
        public float CurrentSpeed => _speed;
        public float CurrentDistance => _distance;
        public float NormalizedProgress =>
            track != null && track.TotalLength > 0f ? Mathf.Clamp01(_distance / track.TotalLength) : 0f;

        public void BeginRun()
        {
            _isRunning = true;
            ResetToStart();
        }

        public void StopRun()
        {
            _isRunning = false;
            _speed = 0f;
        }

        public void ResetToStart()
        {
            _distance = 0f;
            _speed = 0f;
            ApplyPose();
        }

        private void Reset()
        {
            xrOrigin = transform;
        }

        private void Update()
        {
            if (!_isRunning) return;
            if (track == null || xrOrigin == null) return;

            float intensity = input != null ? input.Intensity : 0f;
            float target = (intensity >= startThreshold) ? intensity * maxSpeed : 0f;

            float tau = target > _speed ? accelTime : decelTime;
            float a = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, tau));
            _speed = Mathf.Lerp(_speed, target, a);

            _distance += _speed * Time.deltaTime;

            if (!track.loop)
                _distance = Mathf.Clamp(_distance, 0f, track.TotalLength);

            ApplyPose();
        }

        private void ApplyPose()
        {
            Vector3 pos = track.GetPositionAtDistance(_distance);
            xrOrigin.position = pos;

            if (faceTrackDirection)
            {
                Vector3 dir = track.GetDirectionAtDistance(_distance);
                if (invertTrackDirection) dir = -dir;
                dir.y = 0f;
                if (dir.sqrMagnitude > 1e-4f)
                {
                    Quaternion target = Quaternion.LookRotation(dir, Vector3.up);
                    xrOrigin.rotation = turnSpeed <= 0f
                        ? target
                        : Quaternion.RotateTowards(xrOrigin.rotation, target, turnSpeed * Time.deltaTime);
                }
            }
        }
    }
}
