using UnityEngine;

public class TargetHitDetector : MonoBehaviour
{
    [Header("Score Settings")]
    [SerializeField]
    private ArcheryGameManager gameManager;
    [SerializeField]
    private float targetRadius = 0.5f;

    public void RegisterHit(Vector3 hitWorldPos)
    {
        Vector3 localHit = transform.InverseTransformPoint(hitWorldPos);
        float distance = new Vector2(localHit.x, localHit.y).magnitude;
        float ratio = distance / targetRadius;

        int score = CalculateScore(ratio);
        if (score > 0)
        {
            gameManager.AddScore(score);
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
}
