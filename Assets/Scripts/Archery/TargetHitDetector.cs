using UnityEngine;

public class TargetHitDetector : MonoBehaviour
{
    [Header("Score Settings")]
    [SerializeField]
    private ArcheryGameManager gameManager;
    [SerializeField]
    private ArcheryUI ui;
    [SerializeField]
    private float targetRadius = 0.5f;
    [SerializeField]
    [Range(0.05f, 0.4f)]
    private float bullseyeRatio = 0.15f;

    public void RegisterHit(Vector3 hitWorldPos)
    {
        Vector3 localHit = transform.InverseTransformPoint(hitWorldPos);
        float distance = new Vector2(localHit.x, localHit.y).magnitude;
        float ratio = distance / targetRadius;

        Debug.Log($"[Target] localDist={distance:F3}, ratio={ratio:F2}, targetRadius={targetRadius}");

        int score = CalculateScore(ratio);
        if (score > 0)
        {
            gameManager.AddScore(score);
            ui.ShowScorePopup(score, hitWorldPos);
            Debug.Log($"Hit! +{score}");
        }
    }

    private int CalculateScore(float ratio)
    {
        if (ratio > 1f) return 0;
        if (ratio <= bullseyeRatio) return 10;

        float adjusted = (ratio - bullseyeRatio) / (1f - bullseyeRatio);
        return Mathf.Max(1, 9 - Mathf.FloorToInt(adjusted * 9));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, targetRadius);
    }
}
