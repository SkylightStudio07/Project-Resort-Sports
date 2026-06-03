using UnityEngine;

public class TargetHitDetector : MonoBehaviour
{
    [Header("Score Settings")]
    [SerializeField]
    private ArcheryGameManager gameManager;
    [SerializeField]
    private float targetRadius = 0.5f;
    [SerializeField]
    [Range(0.05f, 0.4f)]
    private float bullseyeRatio = 0.15f;

    [Header("Hit Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip greatClip;
    [SerializeField] private AudioClip perfectClip;

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
            PlayHitSound(score);
            Debug.Log($"Hit! +{score}");
        }
    }

    private void PlayHitSound(int score)
    {
        if (audioSource == null) return;

        if (score == 10 && perfectClip != null)
            audioSource.PlayOneShot(perfectClip);
        else if (score >= 8 && greatClip != null)
            audioSource.PlayOneShot(greatClip);
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
