using System.Collections;
using UnityEngine;

// 볼 복귀 및 10프레임 진행 흐름을 제어
// BowlingBallMovement / PinManager / ScoreManager를 연결
public class BallManager : MonoBehaviour
{
    [Header("컴포넌트 참조")]
    public BowlingBallMovement ball;
    public PinManager          pinManager;
    public ScoreManager        scoreManager;

    [Header("볼 복귀 설정")]
    [Tooltip("볼이 레인 끝 도달 후 핀 집계까지 대기 시간 (초)")]
    public float settleWaitTime = 2f;

    [Tooltip("핀 집계 후 볼이 스폰 포인트로 돌아오기까지 대기 시간 (초)")]
    public float returnDelay = 1f;

    [Tooltip("볼이 복귀할 위치")]
    public Transform spawnPoint;

    private bool _ballInFlight = false;


    private void Start()
    {
        // ScoreManager 이벤트 구독
        scoreManager.OnPinResetNeeded += HandlePinResetNeeded;
        scoreManager.OnGameOver       += HandleGameOver;
    }

    private void OnDestroy()
    {
        scoreManager.OnPinResetNeeded -= HandlePinResetNeeded;
        scoreManager.OnGameOver       -= HandleGameOver;
    }

    // BowlingBallMovement에서 투구 직후 호출
    public void OnBallThrown()
    {
        if (_ballInFlight) return;
        _ballInFlight = true;
        ball.SetGrabbable(false); // 투구 중 잡기 비활성화
        Debug.Log("[BallManager] 볼 투구 시작");
    }

    // LaneEndTrigger에서 볼이 레인 끝에 도달했을 때 호출
    public void OnBallReachedEnd()
    {
        if (!_ballInFlight) return;
        StartCoroutine(ProcessThrow());
    }


    private IEnumerator ProcessThrow()
    {
        // 핀이 완전히 멈출 때까지 대기
        yield return new WaitForSeconds(settleWaitTime);

        // 핀 집계 → ScoreManager에 기록
        // (AdvanceFrame 내부에서 OnPinResetNeeded 이벤트 발생 가능)
        bool isSecondThrow = (scoreManager.ThrowInFrame == 1);
        int  knocked       = pinManager.CountFallenThisThrow();
        scoreManager.RecordThrow(knocked);

        // 1구였고 스트라이크가 아닌 경우 → 2구 준비 스냅샷
        if (!isSecondThrow && knocked < 10)
            pinManager.SnapshotForSecondThrow();

        // 볼 복귀
        yield return new WaitForSeconds(returnDelay);
        ReturnBall();
    }

    private void ReturnBall()
    {
        if (scoreManager.IsGameOver) return;

        _ballInFlight = false;
        ball.ReturnToSpawn(spawnPoint);
        ball.SetGrabbable(true);
        Debug.Log("[BallManager] 볼 복귀 완료 - 다음 투구 준비");
    }


    private void HandlePinResetNeeded()
    {
        pinManager.ResetAllPins();
        Debug.Log("[BallManager] 핀 리셋 완료");
    }

    private void HandleGameOver(int finalScore, string rank)
    {
        _ballInFlight = false;
        ball.SetGrabbable(false);
        Debug.Log($"[BallManager] 게임 종료 처리 - 점수: {finalScore} / 랭크: {rank}");
        // TODO: 결과 UI 표시
    }
}
