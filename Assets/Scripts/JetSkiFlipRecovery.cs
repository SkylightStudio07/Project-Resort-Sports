using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("JetSki/Flip Recovery")]
public class JetSkiFlipRecovery : MonoBehaviour
{
    [Header("Scene References")]
    public JetSkiController jetSki;
    public Rigidbody jetSkiRigidbody;
    public JetSkiScoreManager scoreManager;

    [Header("Detection")]
    public bool recoverDuringGameOver = true;
    [Tooltip("Lower values require the jet ski to be more upside-down before recovery starts.")]
    public float flippedDotThreshold = 0.15f;
    public float secondsBeforeRecovery = 1.2f;

    [Header("Recovery")]
    public float recoveryHeightOffset = 0.6f;
    public bool disableControllerDuringRecovery = true;

    [Header("VR Fade")]
    public float fadeOutSeconds = 0.25f;
    public float holdBlackSeconds = 0.1f;
    public float fadeInSeconds = 0.35f;
    public float fadeDistance = 0.55f;
    public Vector2 fadeCanvasSize = new Vector2(2000f, 2000f);
    public float fadeWorldScale = 0.001f;

    private CanvasGroup fadeGroup;
    private RectTransform fadeRect;
    private Camera fadeCamera;
    private float flippedSeconds;
    private bool isRecovering;

    void Awake()
    {
        ResolveReferences();
    }

    void Update()
    {
        ResolveReferences();

        if (isRecovering || jetSki == null)
            return;

        if (!recoverDuringGameOver && scoreManager != null && scoreManager.IsGameOver)
        {
            flippedSeconds = 0f;
            return;
        }

        bool isFlipped = Vector3.Dot(jetSki.transform.up, Vector3.up) <= flippedDotThreshold;
        flippedSeconds = isFlipped ? flippedSeconds + Time.deltaTime : 0f;

        if (flippedSeconds >= secondsBeforeRecovery)
            StartCoroutine(RecoverRoutine());
    }

    [ContextMenu("Recover Now")]
    public void RecoverNow()
    {
        if (!isRecovering)
            StartCoroutine(RecoverRoutine());
    }

    private IEnumerator RecoverRoutine()
    {
        isRecovering = true;
        flippedSeconds = 0f;

        BuildFadeCanvas();
        yield return FadeTo(1f, fadeOutSeconds);

        bool controllerWasEnabled = jetSki != null && jetSki.enabled;
        if (disableControllerDuringRecovery && jetSki != null)
            jetSki.enabled = false;

        ResetJetSkiPose();

        if (holdBlackSeconds > 0f)
            yield return new WaitForSecondsRealtime(holdBlackSeconds);

        if (disableControllerDuringRecovery && jetSki != null)
            jetSki.enabled = controllerWasEnabled;

        yield return FadeTo(0f, fadeInSeconds);

        if (fadeGroup != null)
            fadeGroup.gameObject.SetActive(false);

        isRecovering = false;
    }

    private void ResetJetSkiPose()
    {
        ResolveReferences();
        if (jetSki == null)
            return;

        Transform target = jetSki.transform;
        Vector3 position = target.position + Vector3.up * recoveryHeightOffset;
        Quaternion rotation = GetUprightRotation(target);

        if (jetSkiRigidbody != null)
        {
            jetSkiRigidbody.velocity = Vector3.zero;
            jetSkiRigidbody.angularVelocity = Vector3.zero;
            jetSkiRigidbody.position = position;
            jetSkiRigidbody.rotation = rotation;
            target.SetPositionAndRotation(position, rotation);
            Physics.SyncTransforms();
            jetSkiRigidbody.velocity = Vector3.zero;
            jetSkiRigidbody.angularVelocity = Vector3.zero;
            jetSkiRigidbody.Sleep();
            return;
        }

        target.SetPositionAndRotation(position, rotation);
        Physics.SyncTransforms();
    }

    private Quaternion GetUprightRotation(Transform target)
    {
        Vector3 forward = Vector3.ProjectOnPlane(target.forward, Vector3.up);

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.ProjectOnPlane(target.right, Vector3.up);

        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeGroup == null)
            yield break;

        fadeGroup.gameObject.SetActive(true);
        float startAlpha = fadeGroup.alpha;

        if (duration <= 0f)
        {
            fadeGroup.alpha = targetAlpha;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        fadeGroup.alpha = targetAlpha;
    }

    private void BuildFadeCanvas()
    {
        Camera targetCamera = GetTargetCamera();

        if (fadeGroup != null)
        {
            AttachFadeCanvas(targetCamera);
            fadeGroup.gameObject.SetActive(true);
            return;
        }

        GameObject canvasObject = new GameObject("JetSki Flip Recovery Fade", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 10000;

        fadeGroup = canvasObject.GetComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
        fadeGroup.interactable = false;

        fadeRect = canvasObject.GetComponent<RectTransform>();
        fadeRect.sizeDelta = fadeCanvasSize;

        GameObject imageObject = new GameObject("Black", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        AttachFadeCanvas(targetCamera);
        canvasObject.SetActive(true);
    }

    private void AttachFadeCanvas(Camera targetCamera)
    {
        if (fadeGroup == null)
            return;

        if (fadeRect == null)
            fadeRect = fadeGroup.GetComponent<RectTransform>();

        fadeCamera = targetCamera;
        Transform fadeTransform = fadeGroup.transform;

        if (fadeCamera != null)
        {
            fadeTransform.SetParent(fadeCamera.transform, false);
            fadeTransform.localPosition = new Vector3(0f, 0f, fadeDistance);
            fadeTransform.localRotation = Quaternion.identity;
        }
        else
        {
            fadeTransform.SetParent(transform, false);
            fadeTransform.localPosition = transform.InverseTransformDirection(transform.forward) * fadeDistance;
            fadeTransform.localRotation = Quaternion.identity;
        }

        fadeTransform.localScale = Vector3.one * fadeWorldScale;
        fadeRect.sizeDelta = fadeCanvasSize;
    }

    private Camera GetTargetCamera()
    {
        if (fadeCamera != null && fadeCamera.isActiveAndEnabled)
            return fadeCamera;

        if (Camera.main != null)
            return Camera.main;

        return FindAnyObjectByType<Camera>();
    }

    private void ResolveReferences()
    {
        if (jetSki == null)
            jetSki = GetComponentInParent<JetSkiController>();

        if (jetSki == null)
            jetSki = FindAnyObjectByType<JetSkiController>();

        if (jetSkiRigidbody == null && jetSki != null)
            jetSkiRigidbody = jetSki.GetComponent<Rigidbody>();

        if (scoreManager == null)
            scoreManager = JetSkiScoreManager.Instance;

        if (scoreManager == null)
            scoreManager = FindAnyObjectByType<JetSkiScoreManager>();
    }
}
