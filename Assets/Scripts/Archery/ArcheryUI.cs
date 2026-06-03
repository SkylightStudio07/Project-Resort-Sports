using TMPro;
using UnityEngine;

public class ArcheryUI : MonoBehaviour
{
    [Header("HUD Settings")]
    [SerializeField] private TextMeshProUGUI setText;
    [SerializeField] private TextMeshProUGUI arrowScoreText;
    [SerializeField] private TextMeshProUGUI arrowsText;
    [SerializeField] private TextMeshProUGUI totalScoreText;
    [Header("Final Score")]
    [SerializeField] private GameObject finalScorePanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private GameObject hudWindow;

    [Header("Settings")]
    [SerializeField] private ArcheryGameManager gameManager;

    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        if (gameManager == null)
            gameManager = FindObjectOfType<ArcheryGameManager>();
    }

    private void Start()
    {
        if (canvas != null) canvas.enabled = false;
        if (finalScorePanel != null) finalScorePanel.SetActive(false);
        if (hudWindow != null) hudWindow.SetActive(true);
    }

    public void ShowHUD()
    {
        if (canvas != null) canvas.enabled = true;
        if (hudWindow != null) hudWindow.SetActive(true);
        if (finalScorePanel != null) finalScorePanel.SetActive(false);
    }

    public void UpdateUI()
    {
        if (gameManager == null) return;
        setText.text = $"Round    {gameManager.CurrentSet + 1} / {gameManager.TotalSets}";
        arrowScoreText.text = $"Score    {gameManager.LastArrowScore}";
        arrowScoreText.color = GetScoreColor(gameManager.LastArrowScore);
        arrowsText.text = $"Arrows    {gameManager.ArrowsLeft}";
        totalScoreText.text = $"Total    {gameManager.TotalScore}";
        if (finalScorePanel != null) finalScorePanel.SetActive(false);
        if (hudWindow != null) hudWindow.SetActive(true);
    }

    public void HideAll()
    {
        if (canvas != null) canvas.enabled = false;
        if (finalScorePanel != null) finalScorePanel.SetActive(false);
    }

    private Color GetScoreColor(int score)
    {
        if (score >= 9) return Color.yellow;
        if (score >= 7) return Color.red;
        if (score >= 5) return new Color(0.53f, 0.81f, 0.98f);
        if (score >= 3) return Color.black;
        return Color.white;
    }

    public void ShowFinalScore(int total)
    {
        if (finalScorePanel == null) return;
        if (finalScoreText != null)
            finalScoreText.text = $"Final Score    {total}";
        if (hudWindow != null) hudWindow.SetActive(false);
        if (canvas != null) canvas.enabled = true;
        finalScorePanel.SetActive(true);
    }
}
