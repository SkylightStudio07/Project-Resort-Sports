using UnityEngine;
using UnityEngine.Events;

public class JetSkiScoreManager : MonoBehaviour
{
    public static JetSkiScoreManager Instance { get; private set; }

    [Header("Course Data")]
    public JetSkiActivityData activityData;

    [Header("Tutorial")]
    public bool showTutorialOnStart = true;
    public bool autoFindTutorialOverlay = true;
    public JetSkiTutorialOverlay tutorialOverlay;

    [Header("Events")]
    [Tooltip("Invoked when score or combo changes. Args: score, combo.")]
    public UnityEvent<int, int> onScoreChanged;
    [Tooltip("Invoked every timer tick. Arg: remaining time.")]
    public UnityEvent<float> onTimerTick;
    [Tooltip("Invoked when the course ends. Args: final score, rank.")]
    public UnityEvent<int, string> onGameOver;

    public int Score { get; private set; }
    public int Combo { get; private set; }
    public float RemainingTime { get; private set; }
    public bool IsRunning { get; private set; }

    private int totalGates;
    private int passedGates;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        ResolveTutorialOverlay();

        if (showTutorialOnStart && tutorialOverlay != null)
            tutorialOverlay.Show();
    }

    public void StartCourse(int gateCount)
    {
        ResolveTutorialOverlay();

        if (tutorialOverlay != null)
            tutorialOverlay.HideForCourseStart();

        totalGates = gateCount;
        passedGates = 0;
        Score = 0;
        Combo = 0;
        IsRunning = true;
        RemainingTime = activityData != null ? activityData.defaultGateTime : 5.9f;
    }

    public void ResetForRetry(bool showTutorial = true)
    {
        ResolveTutorialOverlay();

        totalGates = 0;
        passedGates = 0;
        Score = 0;
        Combo = 0;
        IsRunning = false;
        RemainingTime = activityData != null ? activityData.defaultGateTime : 5.9f;

        onScoreChanged?.Invoke(Score, Combo);
        onTimerTick?.Invoke(RemainingTime);

        if (showTutorial && tutorialOverlay != null)
            tutorialOverlay.Show();
    }

    void ResolveTutorialOverlay()
    {
        if (tutorialOverlay == null && autoFindTutorialOverlay)
            tutorialOverlay = FindAnyObjectByType<JetSkiTutorialOverlay>();

        if (tutorialOverlay == null)
            return;

        tutorialOverlay.manager = this;

        if (tutorialOverlay.activityData == null)
            tutorialOverlay.activityData = activityData;
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

    public void GatePassed(float remainingTime, bool isPerfect)
    {
        if (!IsRunning) return;

        passedGates++;
        Combo++;

        int bonus = activityData != null ? activityData.defaultPerfectBonus : 50;
        int baseScore = activityData != null ? activityData.defaultBaseScore : 100;
        int earned = baseScore + Mathf.RoundToInt(remainingTime * 10f);

        if (isPerfect)
            earned += bonus;

        Score += earned;
        onScoreChanged?.Invoke(Score, Combo);

        if (passedGates >= totalGates)
        {
            EndGame();
            return;
        }

        float nextBase = activityData != null ? activityData.defaultGateTime : 5.9f;
        RemainingTime = nextBase + remainingTime;
    }

    public void GateMissed()
    {
        Combo = 0;
        onScoreChanged?.Invoke(Score, Combo);
    }

    void EndGame()
    {
        IsRunning = false;
        string rank = activityData != null ? activityData.GetRank(Score) : "None";
        Debug.Log($"[ScoreManager] Finished. Score: {Score}, Rank: {rank}");
        onGameOver?.Invoke(Score, rank);
    }
}
