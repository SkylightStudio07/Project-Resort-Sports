using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
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

    [Header("Grip Hold Controls")]
    public bool enableGripHoldControls = true;
    public float gripHoldSeconds = 1.2f;
    [Range(0f, 1f)] public float gripAxisThreshold = 0.75f;

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
    private Image retryGripFill;
    private Image hubGripFill;
    private bool built;
    private bool subscribed;
    private Coroutine retryTutorialCoroutine;
    private float nextButtonActionTime;
    private float retryGripHoldTime;
    private float hubGripHoldTime;

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

        if (enableGripHoldControls && canvasObject != null && canvasObject.activeSelf)
            UpdateGripHoldControls();
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
        ResetGripHoldProgress();

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

        ResetGripHoldProgress();
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
            Hide();
            sessionManager.ReturnToHubWorld();
            return;
        }

        if (string.IsNullOrWhiteSpace(hubWorldSceneName))
        {
            Debug.LogWarning("[JetSkiResultOverlay] Hub world scene name is empty.");
            return;
        }

        Hide();
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
        canvas.worldCamera = GetTargetCamera();
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

        CreateButton(panel.transform, "Retry Button", new Vector2(0f, 0.05f), new Vector2(0.5f, 0.26f), new Vector2(64f, 36f), new Vector2(-24f, -36f), "\uB2E4\uC2DC\uD558\uAE30", Retry);
        CreateButton(panel.transform, "Hub Button", new Vector2(0.5f, 0.05f), new Vector2(1f, 0.26f), new Vector2(24f, 36f), new Vector2(-64f, -36f), "\uD5C8\uBE0C \uC6D4\uB4DC\uB85C", ReturnToHubWorld);

        retryGripFill = CreateGripHoldPrompt(panel.transform, "Retry Grip Hold", new Vector2(0f, 0f), new Vector2(0.5f, 0.08f), new Vector2(64f, 8f), new Vector2(-24f, 2f), "\uC67C\uC190 GRIP \uAE38\uAC8C \uB204\uB974\uAE30");
        hubGripFill = CreateGripHoldPrompt(panel.transform, "Hub Grip Hold", new Vector2(0.5f, 0f), new Vector2(1f, 0.08f), new Vector2(24f, 8f), new Vector2(-64f, 2f), "\uC624\uB978\uC190 GRIP \uAE38\uAC8C \uB204\uB974\uAE30");
    }

    private Image CreateGripHoldPrompt(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        SetRect(rect, anchorMin, anchorMax, offsetMin, offsetMax);

        var text = CreateText(go.transform, "Text", 20, FontStyle.Bold, titleColor);
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        SetRect(text.rectTransform, new Vector2(0f, 0.34f), Vector2.one, Vector2.zero, Vector2.zero);

        var bar = new GameObject("Gauge");
        bar.transform.SetParent(go.transform, false);

        var barRect = bar.AddComponent<RectTransform>();
        SetRect(barRect, new Vector2(0.12f, 0.04f), new Vector2(0.88f, 0.28f), Vector2.zero, Vector2.zero);

        var background = bar.AddComponent<Image>();
        background.sprite = CreateButtonSprite(256, 40, 12);
        background.type = Image.Type.Sliced;
        background.color = new Color(1f, 1f, 1f, 0.42f);
        background.raycastTarget = false;

        var fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(bar.transform, false);

        var fillRect = fillObject.AddComponent<RectTransform>();
        SetRect(fillRect, Vector2.zero, Vector2.one, new Vector2(4f, 4f), new Vector2(-4f, -4f));

        var fill = fillObject.AddComponent<Image>();
        fill.sprite = CreateButtonSprite(256, 32, 10);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f;
        fill.color = accentColor;
        fill.raycastTarget = false;

        return fill;
    }

    private Button CreateButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, string label, UnityAction action)
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
        button.onClick.AddListener(() => InvokeButtonAction(action));

        var text = CreateText(go.transform, "Text", 30, FontStyle.Bold, titleColor);
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 8f), new Vector2(-18f, -8f));

        AddVrInteractableHitbox(go, CalculateStretchedRectSize(parent, anchorMin, anchorMax, offsetMin, offsetMax), action);
        return button;
    }

    private Vector2 CalculateStretchedRectSize(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        Vector2 parentSize = new Vector2(980f, 560f);
        if (parent is RectTransform parentRect)
            parentSize = parentRect.sizeDelta;

        float width = parentSize.x * (anchorMax.x - anchorMin.x) + offsetMax.x - offsetMin.x;
        float height = parentSize.y * (anchorMax.y - anchorMin.y) + offsetMax.y - offsetMin.y;
        return new Vector2(Mathf.Max(1f, width), Mathf.Max(1f, height));
    }

    private void AddVrInteractableHitbox(GameObject buttonObject, Vector2 size, UnityAction action)
    {
        var collider = buttonObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(size.x, size.y, 24f);
        collider.center = Vector3.zero;

        var interactable = buttonObject.AddComponent<XRSimpleInteractable>();
        interactable.selectEntered.AddListener(_ => InvokeButtonAction(action));
    }

    private void InvokeButtonAction(UnityAction action)
    {
        if (Time.unscaledTime < nextButtonActionTime)
            return;

        nextButtonActionTime = Time.unscaledTime + 0.25f;
        ResetGripHoldProgress();
        action?.Invoke();
    }

    private void UpdateGripHoldControls()
    {
        bool leftGripHeld = ReadGrip(XRNode.LeftHand);
        bool rightGripHeld = ReadGrip(XRNode.RightHand);

#if UNITY_EDITOR
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            leftGripHeld |= keyboard.qKey.isPressed;
            rightGripHeld |= keyboard.eKey.isPressed;
        }
#endif

        UpdateGripHold(ref retryGripHoldTime, leftGripHeld, retryGripFill, Retry);
        UpdateGripHold(ref hubGripHoldTime, rightGripHeld, hubGripFill, ReturnToHubWorld);
    }

    private void UpdateGripHold(ref float holdTime, bool isHeld, Image fillImage, UnityAction action)
    {
        if (gripHoldSeconds <= 0f)
        {
            InvokeButtonAction(action);
            return;
        }

        holdTime = isHeld ? holdTime + Time.unscaledDeltaTime : 0f;
        float fill = Mathf.Clamp01(holdTime / gripHoldSeconds);

        if (fillImage != null)
            fillImage.fillAmount = fill;

        if (fill >= 1f)
            InvokeButtonAction(action);
    }

    private bool ReadGrip(XRNode node)
    {
        UnityEngine.XR.InputDevice device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid)
            return false;

        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool gripPressed) && gripPressed)
            return true;

        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out float gripValue) && gripValue >= gripAxisThreshold)
            return true;

        return false;
    }

    private void ResetGripHoldProgress()
    {
        retryGripHoldTime = 0f;
        hubGripHoldTime = 0f;

        if (retryGripFill != null)
            retryGripFill.fillAmount = 0f;

        if (hubGripFill != null)
            hubGripFill.fillAmount = 0f;
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
        var targetCamera = GetTargetCamera();

        if (targetCamera == null)
            return;

        Vector3 toCamera = targetCamera.transform.position - canvasObject.transform.position;
        if (toCamera.sqrMagnitude < 0.0001f)
            return;

        canvasObject.transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
    }

    private Camera GetTargetCamera()
    {
        var targetCamera = Camera.main;
        if (targetCamera == null)
            targetCamera = FindAnyObjectByType<Camera>();

        return targetCamera;
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
