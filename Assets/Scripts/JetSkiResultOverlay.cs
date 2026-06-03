using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

[AddComponentMenu("JetSki/Result Overlay")]
public class JetSkiResultOverlay : MonoBehaviour
{
    private const int SpritePixelsPerUnit = 100;

    [Header("Source")]
    public JetSkiSessionManager sessionManager;
    public JetSkiScoreManager manager;
    public JetSkiCourseStarter courseStarter;
    public JetSkiTutorialOverlay tutorialOverlay;

    [Header("Retry")]
    public Transform gameStartPosition;
    public Transform respawnTarget;
    public bool resetVelocityOnRetry = true;
    public bool keepUprightOnRetry = true;
    public bool showTutorialOnRetry = true;
    public float retryTutorialLockSeconds = 3f;

    [Header("Hub World")]
    public string hubWorldSceneName = "HubWorld";

    [Header("Local Placement")]
    public Vector3 localPosition = new Vector3(0f, 1.08f, 1.65f);
    public Vector3 localEulerAngles = new Vector3(8f, 180f, 0f);
    public Vector2 canvasSize = new Vector2(1100f, 620f);
    public float worldScale = 0.00135f;
    public bool faceMainCamera = false;

    [Header("Dismiss")]
    public bool hideWhenCourseStarts = true;

    [Header("Rank Sounds")]
    public AudioSource rankSfxSource;
    public AudioClip goldResultClip;
    public AudioClip silverResultClip;
    public AudioClip bronzeResultClip;
    public float rankSfxVolume = 1f;

    [Header("Style")]
    public Color panelColor = new Color(0.96f, 0.99f, 1f, 0.9f);
    public Color panelBorderColor = new Color(1f, 1f, 1f, 0.96f);
    public Color accentColor = new Color(0.15f, 0.85f, 1f, 0.96f);
    public Color shadowColor = new Color(0f, 0.05f, 0.1f, 0.24f);
    public Color titleColor = new Color(0.08f, 0.12f, 0.16f, 1f);
    public Color scoreColor = new Color(0.04f, 0.24f, 0.32f, 1f);
    public Color buttonColor = new Color(0.85f, 0.97f, 1f, 0.92f);
    public Color buttonPressedColor = new Color(0.45f, 0.88f, 1f, 0.96f);

    private GameObject canvasObject;
    private CanvasGroup canvasGroup;
    private Text titleText;
    private Text rankText;
    private Text scoreText;
    private bool built;
    private bool subscribed;
    private Coroutine retryTutorialCoroutine;

    void Reset()
    {
        sessionManager = GetComponentInParent<JetSkiSessionManager>();
        manager = JetSkiScoreManager.Instance;
        respawnTarget = GetComponentInParent<JetSkiController>()?.transform;
        tutorialOverlay = GetComponentInParent<JetSkiTutorialOverlay>();
    }

    void Awake()
    {
        ResolveReferences();
    }

    void Start()
    {
        ResolveReferences();
        BuildIfNeeded();
        Subscribe();
        Hide();
    }

    void OnDestroy()
    {
        if (subscribed && manager != null)
            manager.onGameOver.RemoveListener(ShowResult);
    }

    void Update()
    {
        if (hideWhenCourseStarts && manager != null && manager.IsRunning)
            Hide();

        if (faceMainCamera && canvasObject != null && canvasObject.activeSelf)
            FaceCamera();
    }

    public void ShowResult(int finalScore, string rank)
    {
        BuildIfNeeded();

        if (titleText != null)
            titleText.text = "\uC644\uC8FC \uACB0\uACFC";

        if (rankText != null)
            rankText.text = string.IsNullOrEmpty(rank) ? "Rank: None" : $"Rank: {rank}";

        if (scoreText != null)
            scoreText.text = finalScore.ToString("N0");

        canvasObject.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        PlayRankSound(rank);
    }

    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (canvasObject != null)
            canvasObject.SetActive(false);
    }

    private void PlayRankSound(string rank)
    {
        AudioClip clip = GetRankClip(rank);
        if (clip == null)
            return;

        if (rankSfxSource == null)
            rankSfxSource = gameObject.AddComponent<AudioSource>();

        rankSfxSource.playOnAwake = false;
        rankSfxSource.loop = false;
        rankSfxSource.spatialBlend = 1f;
        rankSfxSource.Stop();
        rankSfxSource.PlayOneShot(clip, rankSfxVolume);
    }

    private AudioClip GetRankClip(string rank)
    {
        if (string.IsNullOrWhiteSpace(rank))
            return null;

        switch (rank.Trim().ToLowerInvariant())
        {
            case "gold":
                return goldResultClip;
            case "silver":
                return silverResultClip;
            case "bronze":
                return bronzeResultClip;
            default:
                return null;
        }
    }

    public void Retry()
    {
        ResolveReferences();
        Hide();

        if (sessionManager != null)
        {
            sessionManager.RestartSession();
            return;
        }

        if (manager != null)
            manager.ResetForRetry(false);

        MoveToStartPosition();

        if (courseStarter != null)
            courseStarter.PrepareRetry(respawnTarget);

        if (showTutorialOnRetry)
        {
            TryShowRetryTutorial(true);

            if (retryTutorialCoroutine != null)
                StopCoroutine(retryTutorialCoroutine);

            retryTutorialCoroutine = StartCoroutine(ShowTutorialAfterRetry());
        }
    }

    public void ReturnToHubWorld()
    {
        ResolveReferences();

        if (sessionManager != null)
        {
            sessionManager.ReturnToHubWorld();
            return;
        }

        if (string.IsNullOrWhiteSpace(hubWorldSceneName))
        {
            Debug.LogWarning("[JetSkiResultOverlay] Hub world scene name is empty.");
            return;
        }

        SceneManager.LoadScene(hubWorldSceneName);
    }

    [ContextMenu("Rebuild Result Canvas")]
    public void Rebuild()
    {
        if (canvasObject != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(canvasObject);
            else
#endif
                Destroy(canvasObject);
        }

        built = false;
        BuildIfNeeded();
        Hide();
    }

    private void ResolveReferences()
    {
        if (sessionManager == null)
            sessionManager = GetComponentInParent<JetSkiSessionManager>();

        if (sessionManager == null)
            sessionManager = FindAnyObjectByType<JetSkiSessionManager>();

        if (manager == null)
            manager = JetSkiScoreManager.Instance;

        if (courseStarter == null)
            courseStarter = FindAnyObjectByType<JetSkiCourseStarter>();

        if (tutorialOverlay == null)
            tutorialOverlay = GetComponentInParent<JetSkiTutorialOverlay>(true);

        if (tutorialOverlay == null && respawnTarget != null)
            tutorialOverlay = respawnTarget.GetComponentInChildren<JetSkiTutorialOverlay>(true);

        if (tutorialOverlay == null)
            tutorialOverlay = FindTutorialOverlayInScene();

        if (respawnTarget == null)
            respawnTarget = GetComponentInParent<JetSkiController>()?.transform;

        if (respawnTarget == null)
            respawnTarget = FindAnyObjectByType<JetSkiController>()?.transform;

        if (sessionManager != null)
        {
            if (sessionManager.scoreManager == null)
                sessionManager.scoreManager = manager;
            if (sessionManager.courseStarter == null)
                sessionManager.courseStarter = courseStarter;
            if (sessionManager.tutorialOverlay == null)
                sessionManager.tutorialOverlay = tutorialOverlay;
            if (sessionManager.gameStartPosition == null)
                sessionManager.gameStartPosition = gameStartPosition;
            if (sessionManager.respawnTarget == null)
                sessionManager.respawnTarget = respawnTarget;
            if (string.IsNullOrWhiteSpace(sessionManager.hubWorldSceneName))
                sessionManager.hubWorldSceneName = hubWorldSceneName;
        }
    }

    private IEnumerator ShowTutorialAfterRetry()
    {
        yield return null;
        yield return new WaitForFixedUpdate();

        ResolveReferences();

        if (manager != null)
            manager.ResetForRetry(false);

        if (courseStarter != null)
            courseStarter.PrepareRetry(respawnTarget);

        if (TryShowRetryTutorial(false))
        {
            Debug.Log("[JetSkiResultOverlay] Retry tutorial shown.");
        }
        else
        {
            Debug.LogWarning("[JetSkiResultOverlay] Retry requested tutorial, but no JetSkiTutorialOverlay was found.");
        }

        retryTutorialCoroutine = null;
    }

    private bool TryShowRetryTutorial(bool logMissing)
    {
        ResolveReferences();

        if (tutorialOverlay == null)
        {
            if (logMissing)
                Debug.LogWarning("[JetSkiResultOverlay] Retry requested tutorial, but no JetSkiTutorialOverlay was found.");

            return false;
        }

        tutorialOverlay.ShowForRetry(retryTutorialLockSeconds);
        return true;
    }

    private JetSkiTutorialOverlay FindTutorialOverlayInScene()
    {
        foreach (var overlay in Resources.FindObjectsOfTypeAll<JetSkiTutorialOverlay>())
        {
            if (overlay == null) continue;
            if (!overlay.gameObject.scene.IsValid()) continue;
            return overlay;
        }

        return null;
    }

    private void Subscribe()
    {
        if (subscribed || manager == null) return;
        manager.onGameOver.AddListener(ShowResult);
        subscribed = true;
    }

    private void MoveToStartPosition()
    {
        if (gameStartPosition == null || respawnTarget == null)
        {
            Debug.LogWarning("[JetSkiResultOverlay] Retry needs both Game Start Position and Respawn Target.");
            return;
        }

        Vector3 respawnPosition = gameStartPosition.position;
        Quaternion respawnRotation = GetRespawnRotation();
        var rb = respawnTarget.GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (resetVelocityOnRetry)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.position = respawnPosition;
            rb.rotation = respawnRotation;
            respawnTarget.SetPositionAndRotation(respawnPosition, respawnRotation);
            Physics.SyncTransforms();

            if (resetVelocityOnRetry)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.Sleep();
        }
        else
        {
            respawnTarget.SetPositionAndRotation(respawnPosition, respawnRotation);
            Physics.SyncTransforms();
        }
    }

    private Quaternion GetRespawnRotation()
    {
        if (!keepUprightOnRetry)
            return gameStartPosition.rotation;

        Vector3 forward = Vector3.ProjectOnPlane(gameStartPosition.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.ProjectOnPlane(respawnTarget.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    private void BuildIfNeeded()
    {
        if (built) return;
        built = true;

        canvasObject = new GameObject("JetSki Result Canvas");
        canvasObject.transform.SetParent(transform, false);

        var canvasRect = canvasObject.AddComponent<RectTransform>();
        canvasRect.localPosition = localPosition;
        canvasRect.localRotation = Quaternion.Euler(localEulerAngles);
        canvasRect.localScale = Vector3.one * worldScale;
        canvasRect.sizeDelta = canvasSize;

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 5100;

        canvasObject.AddComponent<GraphicRaycaster>();
        canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();

        canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        BuildPanel(canvasObject.transform);
    }

    private void BuildPanel(Transform parent)
    {
        var panel = new GameObject("Result Panel");
        panel.transform.SetParent(parent, false);

        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(980f, 560f);

        var panelImage = panel.AddComponent<Image>();
        panelImage.sprite = CreatePanelSprite(512, 256, 28);
        panelImage.type = Image.Type.Sliced;

        titleText = CreateText(panel.transform, "Title", 52, FontStyle.Bold, titleColor);
        SetRect(titleText.rectTransform, new Vector2(0f, 0.76f), Vector2.one, new Vector2(70f, -26f), new Vector2(-70f, -28f));
        titleText.alignment = TextAnchor.MiddleCenter;

        rankText = CreateText(panel.transform, "Rank", 44, FontStyle.Bold, accentColor);
        SetRect(rankText.rectTransform, new Vector2(0f, 0.55f), new Vector2(1f, 0.78f), new Vector2(70f, 0f), new Vector2(-70f, -10f));
        rankText.alignment = TextAnchor.MiddleCenter;

        var scoreLabel = CreateText(panel.transform, "Score Label", 26, FontStyle.Normal, titleColor);
        scoreLabel.text = "SCORE";
        scoreLabel.alignment = TextAnchor.MiddleCenter;
        SetRect(scoreLabel.rectTransform, new Vector2(0f, 0.43f), new Vector2(1f, 0.53f), new Vector2(70f, 0f), new Vector2(-70f, 0f));

        scoreText = CreateText(panel.transform, "Score", 74, FontStyle.Bold, scoreColor);
        scoreText.alignment = TextAnchor.MiddleCenter;
        SetRect(scoreText.rectTransform, new Vector2(0f, 0.24f), new Vector2(1f, 0.44f), new Vector2(70f, -4f), new Vector2(-70f, 6f));

        CreateButton(panel.transform, "Retry Button", new Vector2(0f, 0f), new Vector2(0.5f, 0.22f), new Vector2(64f, 42f), new Vector2(-24f, -38f), "\uB2E4\uC2DC\uD558\uAE30", Retry);
        CreateButton(panel.transform, "Hub Button", new Vector2(0.5f, 0f), new Vector2(1f, 0.22f), new Vector2(24f, 42f), new Vector2(-64f, -38f), "\uD5C8\uBE0C \uC6D4\uB4DC\uB85C", ReturnToHubWorld);
    }

    private Button CreateButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, string label, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        SetRect(rect, anchorMin, anchorMax, offsetMin, offsetMax);

        var image = go.AddComponent<Image>();
        image.sprite = CreateButtonSprite(256, 72, 18);
        image.type = Image.Type.Sliced;
        image.color = Color.white;

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.92f, 1f, 1f, 1f);
        colors.pressedColor = buttonPressedColor;
        colors.selectedColor = new Color(0.9f, 1f, 1f, 1f);
        button.colors = colors;
        button.onClick.AddListener(action);

        var text = CreateText(go.transform, "Text", 30, FontStyle.Bold, titleColor);
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 8f), new Vector2(-18f, -8f));

        return button;
    }

    private Text CreateText(Transform parent, string name, int fontSize, FontStyle fontStyle, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var text = go.AddComponent<Text>();
        text.font = CreateUiFont(fontSize);
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(16, fontSize - 12);
        text.resizeTextMaxSize = fontSize;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;

        return text;
    }

    private Font CreateUiFont(int size)
    {
        string[] fontNames = { "Malgun Gothic", "Arial", "Noto Sans CJK KR", "Noto Sans KR" };
        return Font.CreateDynamicFontFromOSFont(fontNames, size);
    }

    private void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private void FaceCamera()
    {
        var targetCamera = Camera.main;
        if (targetCamera == null)
            targetCamera = FindAnyObjectByType<Camera>();

        if (targetCamera == null)
            return;

        Vector3 toCamera = targetCamera.transform.position - canvasObject.transform.position;
        if (toCamera.sqrMagnitude < 0.0001f)
            return;

        canvasObject.transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
    }

    private Sprite CreatePanelSprite(int width, int height, int radius)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "Generated JetSki Result Panel";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        var transparent = new Color(0f, 0f, 0f, 0f);
        var pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color color = transparent;
                if (IsInsideRoundedRect(x, y, width, height, radius))
                {
                    float topGlow = Mathf.Clamp01((float)y / height);
                    color = Color.Lerp(panelColor, Color.white, topGlow * 0.1f);

                    bool border = !IsInsideRoundedRect(x, y, width, height, radius - 5);
                    if (border)
                        color = panelBorderColor;

                    if (y > height - 12)
                        color = accentColor;
                }
                else if (IsInsideRoundedRect(x - 10, y + 10, width, height, radius))
                {
                    color = shadowColor;
                }

                pixels[y * width + x] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), SpritePixelsPerUnit, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    private Sprite CreateButtonSprite(int width, int height, int radius)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "Generated JetSki Result Button";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        var transparent = new Color(0f, 0f, 0f, 0f);
        var pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color color = transparent;
                if (IsInsideRoundedRect(x, y, width, height, radius))
                {
                    color = buttonColor;
                    if (!IsInsideRoundedRect(x, y, width, height, radius - 4))
                        color = accentColor;
                }

                pixels[y * width + x] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), SpritePixelsPerUnit, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    private bool IsInsideRoundedRect(int x, int y, int width, int height, int radius)
    {
        if (radius <= 0)
            return x >= 0 && x < width && y >= 0 && y < height;

        if (x < 0 || x >= width || y < 0 || y >= height)
            return false;

        int cx = Mathf.Clamp(x, radius, width - radius - 1);
        int cy = Mathf.Clamp(y, radius, height - radius - 1);
        int dx = x - cx;
        int dy = y - cy;

        return dx * dx + dy * dy <= radius * radius;
    }
}
