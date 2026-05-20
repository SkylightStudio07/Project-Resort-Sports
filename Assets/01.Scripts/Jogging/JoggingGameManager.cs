using ResortSports.Jogging;
using UnityEngine;
using UnityEngine.Events;

namespace ResortSports.Jogging
{
    /// <summary>
    /// 조깅 미니게임의 진행을 총괄. 트랙/플레이어/입력/UI를 와이어링하고
    /// 시작·종료·일시정지·통계(경과시간, 평균속도)를 관리한다.
    /// 씬에 하나만 배치하면 된다.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class JoggingGameManager : MonoBehaviour
    {
        public enum GameState { Idle, Running, Finished }

        [Header("Wiring")]
        public JogTrack track;
        public JoggingPlayerController player;
        public VRShakeJogInput shakeInput;
        public SpeedBillboardUI speedUI;

        [Header("Rules")]
        [Tooltip("이 거리(m)에 도달하면 종료. 0이면 트랙 전체 길이를 사용.")]
        public float targetDistance = 0f;

        [Tooltip("자동으로 시작할지 여부")]
        public bool autoStart = true;

        [Header("Events")]
        public UnityEvent onStarted;
        public UnityEvent onFinished;

        public GameState State { get; private set; } = GameState.Idle;
        public float ElapsedTime { get; private set; }
        public float AverageSpeed =>
            ElapsedTime > 0.01f && player != null ? player.CurrentDistance / ElapsedTime : 0f;

        private float _goalDistance;

        private void Start()
        {
            if (autoStart) BeginRun();
        }

        public void BeginRun()
        {
            if (track == null || player == null)
            {
                Debug.LogWarning("[JoggingGameManager] track/player가 비어 있어 시작할 수 없습니다.");
                return;
            }
            _goalDistance = targetDistance > 0f ? targetDistance : track.TotalLength;
            player.ResetToStart();
            ElapsedTime = 0f;
            State = GameState.Running;
            onStarted?.Invoke();
        }

        public void EndRun()
        {
            if (State == GameState.Finished) return;
            State = GameState.Finished;
            onFinished?.Invoke();
        }

        private void Update()
        {
            if (State != GameState.Running) return;

            ElapsedTime += Time.deltaTime;

            if (player != null && _goalDistance > 0f && player.CurrentDistance >= _goalDistance)
                EndRun();
        }
    }
}
