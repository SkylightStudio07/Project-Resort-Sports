using TMPro;
using UnityEngine;

public class ArcheryUI : MonoBehaviour
{
    [Header("HUD Settings")]
    [SerializeField] private TextMeshProUGUI setText;
    [SerializeField] private TextMeshProUGUI arrowScoreText;
    [SerializeField] private TextMeshProUGUI arrowsText;
    [SerializeField] private TextMeshProUGUI totalScoreText;

    [Header("Settings")]
    [SerializeField] private ArcheryGameManager gameManager;

    public void UpdateUI()
    {
        setText.text = $"Round    {gameManager.CurrentSet + 1} / {gameManager.TotalSets}";
        arrowScoreText.text = $"Score    {gameManager.LastArrowScore}";
        arrowsText.text = $"Arrows    {gameManager.ArrowsLeft}";
        totalScoreText.text = $"Total    {gameManager.TotalScore}";
    }
}
