using UnityEngine.UI;
using UnityEngine;

public class ArcheryUI : MonoBehaviour
{
    [Header("HUD Settings")]
    [SerializeField]
    private Text roundText;
    [SerializeField]
    private Text scoreText;
    [SerializeField]
    private Text arrowsText;
    [SerializeField]
    private Text totalScoreText;
    [SerializeField]
    private Text finalScoreText;

    [Header("Settings")]
    [SerializeField]
    private ArcheryGameManager gameManager;

    private void Start()
    {
        finalScoreText.gameObject.SetActive(false);
    }

    public void UpdateUI()
    {
        roundText.text = $"Round {gameManager.CurrentRound} / {gameManager.TotalRounds}";
        scoreText.text = $"Score : {gameManager.RoundScore}";
        arrowsText.text = $"Arrows: {gameManager.ArrowsLeft}";
        totalScoreText.text = $"Total : {gameManager.TotalScore}";
        finalScoreText.gameObject.SetActive(false);
    }

    public void ShowFinalScore(int total)
    {
        finalScoreText.gameObject.SetActive(true);
        finalScoreText.text = $"Final Score: {total}";
    }
}