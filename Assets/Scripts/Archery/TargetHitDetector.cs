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
        if (ratio > 1f)
        {
            return 0;
        }
        return Mathf.Max(1, 10 - Mathf.FloorToInt(ratio * 10));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, targetRadius);
    }
}
