using System;
using UnityEngine;

/// <summary>
/// 핀 10개를 총괄 관리합니다.
/// 핀 집계 타이밍은 BallManager가 제어합니다.
/// 첫 핀 충돌 이벤트(OnFirstPinHit)는 BowlingSoundManager가 구독합니다.
/// </summary>
public class PinManager : MonoBehaviour
{
    [Tooltip("씬에 배치된 핀 10개를 순서대로 할당하세요.")]
    public PinBehavior[] pins = new PinBehavior[10];

    /// <summary>이번 투구에서 핀이 처음 충돌했을 때 1회만 발생합니다.</summary>
    public event Action OnFirstPinHit;

    // 1구 후 쓰러진 핀 수를 기억 (2구 계산용)
    private int  _fallenAfterFirstThrow = 0;
    // 이번 투구에 충돌음을 이미 울렸는지
    private bool _pinHitThisThrow = false;

    private void Start()
    {
        // 각 핀의 충돌 알림을 구독
        foreach (var pin in pins)
            if (pin != null) pin.SetManager(this);
    }

    /// <summary>PinBehavior가 충돌 시 호출합니다. 투구당 1회만 이벤트를 발생시킵니다.</summary>
    public void NotifyPinHit()
    {
        if (_pinHitThisThrow) return;
        _pinHitThisThrow = true;
        OnFirstPinHit?.Invoke();
    }

    /// <summary>
    /// 이번 투구에서 새로 쓰러진 핀 수를 반환합니다.
    /// BallManager에서 타이밍에 맞춰 호출합니다.
    /// </summary>
    public int CountFallenThisThrow()
    {
        int fallenNow        = CountAllFallen();
        int knockedThisThrow = fallenNow - _fallenAfterFirstThrow;

        _fallenAfterFirstThrow = fallenNow;

        Debug.Log($"[PinManager] 이번 투구 쓰러진 핀: {knockedThisThrow}");
        return knockedThisThrow;
    }

    private int CountAllFallen()
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
        _pinHitThisThrow       = false;
        foreach (var pin in pins)
            if (pin != null) pin.ResetPin();

        Debug.Log("[PinManager] 핀 전체 리셋");
    }

    /// <summary>1구 완료 후 카운터만 리셋합니다 (핀은 그대로).</summary>
    public void SnapshotForSecondThrow()
    {
        _fallenAfterFirstThrow = CountAllFallen();
        _pinHitThisThrow       = false; // 2구에서도 충돌음 다시 울리도록
    }
}
