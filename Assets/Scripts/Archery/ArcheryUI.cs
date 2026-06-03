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

    [Header("Settings")]
    [SerializeField] private ArcheryGameManager gameManager;

    private void Start()
    {
        if (finalScorePanel != null)
            finalScorePanel.SetActive(false);
    }

    public void UpdateUI()
    {
        setText.text = $"Round    {gameManager.CurrentSet + 1} / {gameManager.TotalSets}";
        arrowScoreText.text = $"Score    {gameManager.LastArrowScore}";
        arrowsText.text = $"Arrows    {gameManager.ArrowsLeft}";
        totalScoreText.text = $"Total    {gameManager.TotalScore}";
        if (finalScorePanel != null)
            finalScorePanel.SetActive(false);
    }

    public void ShowFinalScore(int total)
    {
        if (finalScorePanel == null) return;
        if (finalScoreText != null)
            finalScoreText.text = $"Final Score    {total}";
        finalScorePanel.SetActive(true);
    }
}
