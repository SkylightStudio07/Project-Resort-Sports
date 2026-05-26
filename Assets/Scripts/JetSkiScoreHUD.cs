using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 제트스키 핸들 위 World Space Canvas 에 붙여서 점수/콤보/타이머/게임오버 표시.
/// JetSkiScoreManager 의 UnityEvent 3종(onScoreChanged / onTimerTick / onGameOver)을 구독한다.
/// 부스트 HUD(JetSkiHUD)와 같은 Canvas에 묶어도 되고, 별도 패널이어도 됨.
/// </summary>
public class JetSkiScoreHUD : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("비워두면 JetSkiScoreManager.Instance 를 자동으로 잡는다.")]
    public JetSkiScoreManager manager;

    [Header("Timer Bar")]
    [Tooltip("Image Type = Filled / Fill Method = Horizontal. 잔여시간 비율로 줄어든다.")]
    public Image timerFill;
    public Color timerLowColor  = new Color(1f, 0.2f, 0.2f);
    public Color timerHighColor = new Color(1f, 0.9f, 0.2f);

    [Header("Texts (TMP)")]
    public TMP_Text scoreText;
    public TMP_Text comboText;

    [Header("Game Over Panel")]
    [Tooltip("게임 오버 시 활성화. 평소엔 비활성으로 둔다.")]
    public GameObject gameOverPanel;
    public TMP_Text finalScoreText;
    public TMP_Text rankText;

    float timerBase = 5.9f;
    bool  subscribed;

    void Start()
    {
        if (manager == null) manager = JetSkiScoreManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[ScoreHUD] JetSkiScoreManager 를 찾지 못했습니다.");
            return;
        }

        manager.onScoreChanged.AddListener(HandleScoreChanged);
        manager.onTimerTick.AddListener(HandleTimerTick);
        manager.onGameOver.AddListener(HandleGameOver);
        subscribed = true;

        if (manager.activityData != null)
            timerBase = Mathf.Max(0.01f, manager.activityData.defaultGateTime);

        // 초기 표시
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (scoreText    != null) scoreText.text = "0";
        if (comboText    != null) comboText.text = "";
        if (timerFill    != null)
        {
            timerFill.fillAmount = 1f;
            timerFill.color      = timerHighColor;
        }
    }

    void OnDestroy()
    {
        if (!subscribed || manager == null) return;
        manager.onScoreChanged.RemoveListener(HandleScoreChanged);
        manager.onTimerTick.RemoveListener(HandleTimerTick);
        manager.onGameOver.RemoveListener(HandleGameOver);
    }

    void HandleScoreChanged(int score, int combo)
    {
        if (scoreText != null) scoreText.text = score.ToString("N0");
        if (comboText != null) comboText.text = combo > 1 ? $"x{combo}" : "";
    }

    void HandleTimerTick(float remaining)
    {
        if (timerFill == null) return;

        // GatePassed 가 잔여시간을 이월시켜 timerBase 를 잠깐 초과할 수 있어 Clamp01.
        float ratio = Mathf.Clamp01(remaining / timerBase);
        timerFill.fillAmount = ratio;
        timerFill.color      = Color.Lerp(timerLowColor, timerHighColor, ratio);
    }

    void HandleGameOver(int finalScore, string rank)
    {
        if (gameOverPanel  != null) gameOverPanel.SetActive(true);
        if (finalScoreText != null) finalScoreText.text = finalScore.ToString("N0");
        if (rankText       != null) rankText.text       = rank;
        if (timerFill      != null)
        {
            timerFill.fillAmount = 0f;
            timerFill.color      = timerLowColor;
        }
    }
}
