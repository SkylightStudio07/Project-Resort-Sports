using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 부이 게이트 하나에 붙이는 컴포넌트.
/// BoxCollider (isTrigger = true) 가 같은 오브젝트에 있어야 함.
/// 태그 불필요 — JetSkiController 컴포넌트로 제트스키를 감지함.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GateController : MonoBehaviour
{
    [Tooltip("두 부이 사이의 실제 너비 (m). 퍼펙트 판정 기준.")]
    public float gateWidth = 6f;

    [Tooltip("게이트 높이 (m). BoxCollider 높이와 맞춰야 통과 감지됨.")]
    public float gateHeight = 4f;

    [Header("이벤트 (선택)")]
    public UnityEvent onPassed;
    public UnityEvent onPerfect;
    public event Action<bool> GatePassedResult;

    public bool IsPassed { get; private set; }

    private JetSkiScoreManager scoreManager;

    void Start()
    {
        scoreManager = JetSkiScoreManager.Instance;

        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsPassed) return;

        // 제트스키인지 확인
        if (other.GetComponentInParent<JetSkiController>() == null) return;

        Debug.Log($"[Gate] {gameObject.name} — 제트스키 진입 감지됨");

        if (scoreManager == null)
        {
            Debug.LogError("[Gate] JetSkiScoreManager가 씬에 없습니다.");
            return;
        }

        if (scoreManager.IsGameOver)
            return;

        // 코스가 안 시작됐으면 자동 시작
        if (!scoreManager.IsRunning)
        {
            Debug.LogWarning("[Gate] 코스가 시작되지 않아 자동 시작합니다.");
            var starter = FindAnyObjectByType<JetSkiCourseStarter>();
            if (starter != null) starter.StartCourse();
            else scoreManager.StartCourse(1); // 스타터 없으면 임시로 1개짜리 코스
        }

        if (!scoreManager.IsRunning)
            return;

        float lateralOffset = Vector3.Dot(other.transform.position - transform.position, transform.right);
        float normalized    = Mathf.Abs(lateralOffset) / (gateWidth * 0.5f);
        float threshold     = scoreManager.activityData != null
                              ? scoreManager.activityData.perfectThreshold
                              : 0.3f;
        bool isPerfect = normalized <= threshold;

        IsPassed = true;
        scoreManager.GatePassed(scoreManager.RemainingTime, isPerfect);

        Debug.Log($"[Gate] {gameObject.name} 통과! 퍼펙트: {isPerfect}  점수: {scoreManager.Score}");
        GatePassedResult?.Invoke(isPerfect);
        onPassed?.Invoke();
        if (isPerfect) onPerfect?.Invoke();
    }

    public void ResetGate() => IsPassed = false;

#if UNITY_EDITOR
    [ContextMenu("Apply Size to BoxCollider")]
    void ApplyColliderSize()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null) box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size      = new Vector3(gateWidth, gateHeight, 0.5f);
        box.center    = Vector3.zero;
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[Gate] BoxCollider 크기 적용: {gateWidth} x {gateHeight}");
    }

    void OnDrawGizmos()
    {
        Gizmos.color = IsPassed ? Color.gray : Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(gateWidth, gateHeight, 0.5f));
    }
#endif
}
