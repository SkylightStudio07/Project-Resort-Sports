using TMPro;
using UnityEngine;

public class ArcheryUI : MonoBehaviour
{
    [Header("HUD Settings")]
    [SerializeField] private TextMeshProUGUI setText;
    [SerializeField] private TextMeshProUGUI arrowScoreText;
    [SerializeField] private TextMeshProUGUI arrowsText;
    [SerializeField] private TextMeshProUGUI totalScoreText;
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Header("Settings")]
    [SerializeField] private ArcheryGameManager gameManager;

    private void Start()
    {
        finalScoreText.gameObject.SetActive(false);
    }

    public void UpdateUI()
    {
        setText.text = $"Set {gameManager.CurrentSet + 1} / {gameManager.TotalSets}";
        arrowScoreText.text = $"Score : {gameManager.LastArrowScore}";
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
