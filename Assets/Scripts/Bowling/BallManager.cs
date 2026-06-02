using System;
using System.Collections;
using UnityEngine;

// 볼 복귀 및 10프레임 진행 흐름을 제어합니다.
public class BallManager : MonoBehaviour
{
    [Header("컴포넌트 참조")]
    public BowlingBallMovement ball;
    public PinManager          pinManager;
    public ScoreManager        scoreManager;

    [Header("볼 복귀 설정")]
    public float     settleWaitTime = 2f;
    public float     returnDelay    = 1f;
    public Transform spawnPoint;

    // ── 이벤트 (UI가 구독) ───────────────────────────────────────────────
    // 거터볼 발생 시 발생합니다.
    public event Action OnGutterBall;

    // 볼이 스폰 포인트로 복귀해 다음 투구 준비가 됐을 때 발생합니다.
    public event Action OnBallReady;

    // ── 내부 상태 ────────────────────────────────────────────────────────
    private bool _ballInFlight = false;
    private bool _gutterThisThrow = false;

    // ── Unity 생명주기 ───────────────────────────────────────────────────

    private void Start()
    {
        scoreManager.OnPinResetNeeded += HandlePinResetNeeded;
        scoreManager.OnGameOver       += HandleGameOver;
        ball.OnGutterEntered          += HandleGutterEntered;
    }

    private void OnDestroy()
    {
        scoreManager.OnPinResetNeeded -= HandlePinResetNeeded;
        scoreManager.OnGameOver       -= HandleGameOver;
        if (ball != null)
            ball.OnGutterEntered      -= HandleGutterEntered;
    }


    public void OnBallThrown()
    {
        if (_ballInFlight) return;
        _ballInFlight     = true;
        _gutterThisThrow  = false;
        ball.SetGrabbable(false);
        Debug.Log("[BallManager] 볼 투구 시작");
    }

    public void OnBallReachedEnd()
    {
        if (!_ballInFlight) return;
        StartCoroutine(ProcessThrow());
    }


    private IEnumerator ProcessThrow()
    {
        yield return new WaitForSeconds(settleWaitTime);

        bool isSecondThrow = (scoreManager.ThrowInFrame == 1);
        int  knocked       = pinManager.CountFallenThisThrow();
        scoreManager.RecordThrow(knocked);

        if (!isSecondThrow && knocked < 10)
            pinManager.SnapshotForSecondThrow();

        yield return new WaitForSeconds(returnDelay);
        ReturnBall();
    }

    private void ReturnBall()
    {
        if (scoreManager.IsGameOver) return;

        _ballInFlight    = false;
        _gutterThisThrow = false;
        ball.ReturnToSpawn(spawnPoint);
        ball.SetGrabbable(true);
        OnBallReady?.Invoke();
        Debug.Log("[BallManager] 볼 복귀 완료 - 다음 투구 준비");
    }

    private void HandleGutterEntered()
    {
        if (_gutterThisThrow) return; // 한 투구에 한 번만
        _gutterThisThrow = true;
        OnGutterBall?.Invoke();
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
        Debug.Log($"[BallManager] 게임 종료 - 점수: {finalScore} / 랭크: {rank}");
    }


    public void ResetGame()
    {
        StopAllCoroutines();
        _ballInFlight    = false;
        _gutterThisThrow = false;

        scoreManager.ResetGame();
        pinManager.ResetAllPins();

        ball.ReturnToSpawn(spawnPoint);
        ball.SetGrabbable(true);
        OnBallReady?.Invoke();
        Debug.Log("[BallManager] 게임 재시작");
    }
}
