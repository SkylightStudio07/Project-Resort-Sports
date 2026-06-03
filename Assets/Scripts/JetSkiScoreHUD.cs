using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JetSkiScoreHUD : MonoBehaviour
{
    [Header("Source")]
    public JetSkiScoreManager manager;

    [Header("Timer Bar")]
    public Image timerFill;
    public Color timerLowColor = new Color(1f, 0.2f, 0.2f);
    public Color timerHighColor = new Color(1f, 0.9f, 0.2f);

    [Header("Texts (TMP)")]
    public TMP_Text scoreText;
    public TMP_Text comboText;

    float timerBase = 5.9f;
    bool subscribed;

    void Start()
    {
        if (manager == null) manager = JetSkiScoreManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[ScoreHUD] JetSkiScoreManager not found.");
            return;
        }

        manager.onScoreChanged.AddListener(HandleScoreChanged);
        manager.onTimerTick.AddListener(HandleTimerTick);
        subscribed = true;

        if (manager.activityData != null)
            timerBase = Mathf.Max(0.01f, manager.activityData.defaultGateTime);

        if (scoreText != null) scoreText.text = "0";
        if (comboText != null) comboText.text = "";
        if (timerFill != null)
        {
            timerFill.fillAmount = 1f;
            timerFill.color = timerHighColor;
        }
    }

    void OnDestroy()
    {
        if (!subscribed || manager == null) return;
        manager.onScoreChanged.RemoveListener(HandleScoreChanged);
        manager.onTimerTick.RemoveListener(HandleTimerTick);
    }

    void HandleScoreChanged(int score, int combo)
    {
        if (scoreText != null) scoreText.text = score.ToString("N0");
        if (comboText != null) comboText.text = combo > 1 ? $"x{combo}" : "";
    }

    void HandleTimerTick(float remaining)
    {
        if (timerFill == null) return;

        float ratio = Mathf.Clamp01(remaining / timerBase);
        timerFill.fillAmount = ratio;
        timerFill.color = Color.Lerp(timerLowColor, timerHighColor, ratio);
    }
}
