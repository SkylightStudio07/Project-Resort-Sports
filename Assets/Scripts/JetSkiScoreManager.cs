using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 제트스키 코스의 타이머·점수·콤보·게임 오버를 관리.
/// 씬에 하나만 배치. GateController들이 이 매니저에 이벤트를 보냄.
/// </summary>
public class JetSkiScoreManager : MonoBehaviour
{
    public static JetSkiScoreManager Instance { get; private set; }

    [Header("코스 데이터")]
    public JetSkiActivityData activityData;

    [Header("이벤트")]
    [Tooltip("점수·콤보 변경 시 (score, combo)")]
    public UnityEvent<int, int> onScoreChanged;
    [Tooltip("타이머 갱신 시 (remainingTime)")]
    public UnityEvent<float> onTimerTick;
    [Tooltip("게임 오버 / 완주 시 (finalScore, rank)")]
    public UnityEvent<int, string> onGameOver;

    public int   Score         { get; private set; }
    public int   Combo         { get; private set; }
    public float RemainingTime { get; private set; }
    public bool  IsRunning     { get; private set; }

    private int totalGates;
    private int passedGates;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>게이트 수를 넘겨서 코스를 시작.</summary>
    public void StartCourse(int gateCount)
    {
        totalGates  = gateCount;
        passedGates = 0;
        Score       = 0;
        Combo       = 0;
        IsRunning   = true;
        RemainingTime = activityData != null ? activityData.defaultGateTime : 5.9f;
    }

    void Update()
    {
        if (!IsRunning) return;

        RemainingTime -= Time.deltaTime;
        onTimerTick?.Invoke(RemainingTime);

        if (RemainingTime <= 0f)
        {
            RemainingTime = 0f;
            EndGame();
        }
    }

    /// <summary>GateController가 통과 시 호출.</summary>
    public void GatePassed(float remainingTime, bool isPerfect)
    {
        if (!IsRunning) return;

        passedGates++;
        Combo++;

        // 점수 = 기본 점수 + 잔여 시간 × 10 (+ 퍼펙트 보너스)
        int bonus    = activityData != null ? activityData.defaultPerfectBonus : 50;
        int base_    = activityData != null ? activityData.defaultBaseScore    : 100;
        int earned   = base_ + Mathf.RoundToInt(remainingTime * 10f);
        if (isPerfect) earned += bonus;

        Score += earned;
        onScoreChanged?.Invoke(Score, Combo);

        if (passedGates >= totalGates)
        {
            EndGame();
            return;
        }

        // 잔여 시간을 다음 게이트 카운터에 이월
        float nextBase = activityData != null ? activityData.defaultGateTime : 5.9f;
        RemainingTime  = nextBase + remainingTime;
    }

    /// <summary>게이트를 놓쳤을 때 (콤보 리셋).</summary>
    public void GateMissed()
    {
        Combo = 0;
        onScoreChanged?.Invoke(Score, Combo);
    }

    void EndGame()
    {
        IsRunning = false;
        string rank = activityData != null ? activityData.GetRank(Score) : "None";
        Debug.Log($"[ScoreManager] 종료 — 점수: {Score}, 랭크: {rank}");
        onGameOver?.Invoke(Score, rank);
    }

    // ── 이벤트 연결 없이도 동작 확인용 OnGUI HUD ──────────────────
    void OnGUI()
    {
        if (!IsRunning && Score == 0) return;

        var style = new GUIStyle(GUI.skin.box)
        {
            fontSize  = 24,
            alignment = TextAnchor.MiddleLeft,
        };
        style.normal.textColor = Color.white;

        string status = IsRunning ? $"⏱ {RemainingTime:F1}s" : "FINISH";
        string text   = $" {status}\n 점수: {Score}\n 콤보: x{Combo}";

        GUI.Box(new Rect(20, 20, 220, 100), text, style);
    }
}
