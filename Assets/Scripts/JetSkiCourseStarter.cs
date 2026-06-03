using UnityEngine;
using UnityEngine.Events;

public class JetSkiCourseStarter : MonoBehaviour
{
    [Header("게이트")]
    [Tooltip("비워두면 하위 오브젝트에서 자동 수집. 에디터에서 'Collect Gates' 버튼으로 미리 채울 수 있음.")]
    public GateController[] gates;

    [Header("시작 방식")]
    [Tooltip("true: 제트스키가 이 오브젝트의 트리거에 진입하면 자동 시작.")]
    public bool autoStartOnTrigger = true;
    public bool ignoreIfRunning = true;
    public bool waitForExitAfterRetry = true;
    public float retryAutoStartBlockSeconds = 1f;

    [Header("이벤트 (선택)")]
    public UnityEvent onCourseStart;
    public UnityEvent onCourseReset;

    private JetSkiScoreManager scoreManager;
    private bool waitingForJetSkiExit;
    private Collider triggerCollider;
    private float blockAutoStartUntil;

    void Start()
    {
        scoreManager = JetSkiScoreManager.Instance;
        if (scoreManager != null)
            scoreManager.onGameOver.AddListener(OnGameOver);

        triggerCollider = GetComponent<Collider>();

        CollectGatesIfEmpty();

        if (gates.Length == 0)
            Debug.LogWarning("[CourseStarter] 게이트가 없습니다. 하위 오브젝트에 GateController를 추가하거나 Inspector에서 직접 할당하세요.");
        else
            Debug.Log($"[CourseStarter] 게이트 {gates.Length}개 준비됨.");
    }

    void CollectGatesIfEmpty()
    {
        if (gates != null && gates.Length > 0) return;
        gates = GetComponentsInChildren<GateController>(includeInactive: true);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!autoStartOnTrigger) return;
        if (other.GetComponentInParent<JetSkiController>() == null) return;
        if (Time.time < blockAutoStartUntil) return;
        if (waitingForJetSkiExit) return;
        StartCourse();
    }

    void OnTriggerExit(Collider other)
    {
        if (!waitingForJetSkiExit) return;
        if (other.GetComponentInParent<JetSkiController>() == null) return;

        waitingForJetSkiExit = false;
    }

    public void StartCourse()
    {
        if (scoreManager == null)
        {
            Debug.LogError("[CourseStarter] JetSkiScoreManager가 씬에 없습니다.");
            return;
        }
        if (ignoreIfRunning && scoreManager.IsRunning) return;
        if (scoreManager.IsGameOver) return;

        CollectGatesIfEmpty();
        ResetAllGates();
        scoreManager.StartCourse(gates.Length);
        onCourseStart?.Invoke();
        Debug.Log($"[CourseStarter] 코스 시작 — 게이트 {gates.Length}개");
    }

    public void ResetCourse()
    {
        ResetAllGates();
        onCourseReset?.Invoke();
    }

    public void PrepareRetry(Transform jetSkiTransform)
    {
        ResetCourse();

        blockAutoStartUntil = Time.time + Mathf.Max(0f, retryAutoStartBlockSeconds);
        waitingForJetSkiExit = waitForExitAfterRetry && IsInsideStartTrigger(jetSkiTransform);
    }

    bool IsInsideStartTrigger(Transform target)
    {
        if (target == null) return false;

        if (triggerCollider == null)
            triggerCollider = GetComponent<Collider>();

        if (triggerCollider == null) return false;
        foreach (var jetSkiCollider in target.GetComponentsInChildren<Collider>())
        {
            if (jetSkiCollider == null || jetSkiCollider == triggerCollider)
                continue;

            if (triggerCollider.bounds.Intersects(jetSkiCollider.bounds))
                return true;
        }

        return triggerCollider.bounds.Contains(target.position);
    }

    void ResetAllGates()
    {
        foreach (var gate in gates)
            if (gate != null) gate.ResetGate();
    }

    void OnGameOver(int finalScore, string rank)
    {
        Debug.Log($"[CourseStarter] 종료 — 점수: {finalScore}, 랭크: {rank}");
        ResetAllGates();
    }

    void OnDestroy()
    {
        if (scoreManager != null)
            scoreManager.onGameOver.RemoveListener(OnGameOver);
    }

    // ── 에디터 전용 ───────────────────────────────────────────────
#if UNITY_EDITOR
    [ContextMenu("Collect Gates From Children")]
    void EditorCollectGates()
    {
        gates = GetComponentsInChildren<GateController>(includeInactive: true);
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[CourseStarter] 게이트 {gates.Length}개 수집됨.");
    }

    void OnDrawGizmos()
    {
        if (gates == null || gates.Length == 0) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < gates.Length - 1; i++)
        {
            if (gates[i] == null || gates[i + 1] == null) continue;
            Gizmos.DrawLine(gates[i].transform.position, gates[i + 1].transform.position);
        }
        if (gates[0]    != null) { Gizmos.color = Color.green; Gizmos.DrawSphere(gates[0].transform.position, 0.4f); }
        if (gates[^1]   != null) { Gizmos.color = Color.red;   Gizmos.DrawSphere(gates[^1].transform.position, 0.4f); }
    }
#endif
}
