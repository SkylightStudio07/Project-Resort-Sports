using System.Collections;
using UnityEngine;

/// <summary>
/// 핀 10개를 총괄 관리합니다.
/// 볼 투구 후 일정 시간 대기했다가 쓰러진 핀 수를 집계해 ScoreManager에 전달합니다.
/// </summary>
public class PinManager : MonoBehaviour
{
    [Tooltip("씬에 배치된 핀 10개를 순서대로 할당하세요.")]
    public PinBehavior[] pins = new PinBehavior[10];

    [Tooltip("투구 후 핀이 완전히 멈출 때까지 기다리는 시간 (초)")]
    public float settleWaitTime = 3f;

    public ScoreManager scoreManager;

    // 1구 후 쓰러진 핀 수를 기억 (2구 계산용)
    private int _fallenAfterFirstThrow = 0;

    /// <summary>BowlingBallMovement에서 투구 직후 호출합니다.</summary>
    public void OnBallThrown()
    {
        StartCoroutine(WaitAndCount());
    }

    private IEnumerator WaitAndCount()
    {
        yield return new WaitForSeconds(settleWaitTime);

        int fallenNow = CountFallen();

        // 이번 투구에서 새로 쓰러진 핀 수
        int knockedThisThrow = fallenNow - _fallenAfterFirstThrow;

        Debug.Log($"[PinManager] 이번 투구 쓰러진 핀: {knockedThisThrow}");

        scoreManager.RecordThrow(knockedThisThrow);

        // 다음 투구를 위해 현재 쓰러진 수 기억
        _fallenAfterFirstThrow = fallenNow;
    }

    private int CountFallen()
    {
        int count = 0;
        foreach (var pin in pins)
            if (pin != null && pin.IsFallen)
                count++;
        return count;
    }

    /// <summary>프레임 시작 시 모든 핀 상태를 초기화합니다.</summary>
    public void ResetAllPins()
    {
        _fallenAfterFirstThrow = 0;
        foreach (var pin in pins)
            if (pin != null) pin.ResetPin();
    }

    /// <summary>1구→2구 전환 시 카운터만 리셋합니다 (핀은 그대로).</summary>
    public void ResetCountForSecondThrow()
    {
        _fallenAfterFirstThrow = CountFallen();
    }
}
