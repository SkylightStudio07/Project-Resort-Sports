using UnityEngine;
using UnityEngine.UI;

public class ScorePopup : MonoBehaviour
{
    [SerializeField]
    private Text scoreText;
    [SerializeField]
    private float floatSpeed = 0.3f;
    [SerializeField]
    private float lifetime = 1.5f;

    private float elapsed = 0f;
    private Color originalColor;
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    public void Init(int score, Vector3 worldPos)
    {
        transform.position = worldPos;
        scoreText.text = $"+{score}";
        originalColor = scoreText.color;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        if (mainCamera != null)
        {
            transform.rotation = Quaternion.LookRotation(mainCamera.transform.position - transform.position);
        }

        float alpha = Mathf.Lerp(1f, 0f, elapsed / lifetime);
        scoreText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
