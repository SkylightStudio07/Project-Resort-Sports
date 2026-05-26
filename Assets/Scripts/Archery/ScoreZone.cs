using UnityEngine;

public class ScoreZone : MonoBehaviour
{
    [Header("Score Settings")]
    [SerializeField]
    private int zoneScore = 10;

    [Header("Settings")]
    [SerializeField]
    private ArcheryGameManager gameManager;

    private bool isHit = false;

    private void OnEnable()
    {
        gameManager.OnRoundStarted += ResetZone;
    }

    private void OnDisable()
    {
        gameManager.OnRoundStarted -= ResetZone;
    }

    private void ResetZone() => isHit = false;

    public void RegisterHit(ArrowController arrow)
    {
        if (isHit) return;
        isHit = true;
        gameManager.AddScore(zoneScore);
        Debug.Log($" Hit! + {zoneScore}");
    }
}