using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("JetSki/Tutorial Overlay")]
public class JetSkiTutorialOverlay : MonoBehaviour
{
    private const int SpritePixelsPerUnit = 100;

    [Header("Source")]
    public JetSkiScoreManager manager;
    public JetSkiActivityData activityData;

    [Header("Local Placement")]
    [Tooltip("Local position of the generated tutorial canvas. Put it above/in front of the jet ski dashboard.")]
    public Vector3 localPosition = new Vector3(0f, 1.25f, 1.8f);

    [Tooltip("Local rotation of the generated tutorial canvas. Y=180 faces a rider sitting behind it.")]
    public Vector3 localEulerAngles = new Vector3(8f, 180f, 0f);

    public Vector2 canvasSize = new Vector2(1200f, 360f);
    public float worldScale = 0.0014f;

    [Tooltip("Rotate the canvas to face the main camera while keeping it attached to this object.")]
    public bool faceMainCamera = false;

    [Header("Dismiss")]
    public bool hideWhenCourseStarts = true;
    public bool allowKeyboardDismiss = true;

    [Header("Style")]
    public Color panelColor = new Color(0.96f, 0.99f, 1f, 0.82f);
    public Color panelBorderColor = new Color(1f, 1f, 1f, 0.95f);
    public Color accentColor = new Color(0.15f, 0.85f, 1f, 0.95f);
    public Color shadowColor = new Color(0f, 0.05f, 0.1f, 0.22f);
    public Color titleColor = new Color(0.08f, 0.12f, 0.16f, 1f);
    public Color bodyColor = new Color(0.16f, 0.21f, 0.26f, 1f);

    private GameObject canvasObject;
    private RectTransform canvasRect;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Text titleText;
    private Text bodyText;
    private bool built;
    private float visibleLockUntil;

    void Reset()
    {
        manager = JetSkiScoreManager.Instance;
    }

    void Awake()
    {
        if (manager == null)
            manager = JetSkiScoreManager.Instance;

        if (activityData == null && manager != null)
            activityData = manager.activityData;
    }

    void Start()
    {
        BuildIfNeeded();
        ApplyData();
    }

    void Update()
    {
        if (hideWhenCourseStarts && manager != null && manager.IsRunning && Time.unscaledTime >= visibleLockUntil)
            Hide();

        if (allowKeyboardDismiss && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)))
            Hide();

        if (faceMainCamera && canvasObject != null && canvasObject.activeSelf)
            FaceCamera();
    }

    public void Show()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        BuildIfNeeded();
        ApplyData();

        canvasObject.SetActive(true);
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    public void ShowForRetry(float lockVisibleSeconds = 1.5f)
    {
        visibleLockUntil = Time.unscaledTime + Mathf.Max(0f, lockVisibleSeconds);
        Show();
    }

    public void HideForCourseStart()
    {
        if (Time.unscaledTime < visibleLockUntil)
            return;

        Hide();
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

    [ContextMenu("Rebuild Tutorial Canvas")]
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
        ApplyData();
    }

    private void BuildIfNeeded()
    {
        if (built) return;
        built = true;

        canvasObject = new GameObject("JetSki Tutorial Canvas");
        canvasObject.transform.SetParent(transform, false);

        canvasRect = canvasObject.AddComponent<RectTransform>();
        canvasRect.localPosition = localPosition;
        canvasRect.localRotation = Quaternion.Euler(localEulerAngles);
        canvasRect.localScale = Vector3.one * worldScale;
        canvasRect.sizeDelta = canvasSize;

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 5000;

        canvasObject.AddComponent<GraphicRaycaster>();
        canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        var panel = new GameObject("Tutorial Panel");
        panel.transform.SetParent(canvasObject.transform, false);

        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(1080f, 320f);

        var panelImage = panel.AddComponent<Image>();
        panelImage.sprite = CreatePanelSprite(512, 128, 26);
        panelImage.type = Image.Type.Sliced;
        panelImage.pixelsPerUnitMultiplier = 1f;

        titleText = CreateText(panel.transform, "Title", 40, FontStyle.Bold, titleColor);
        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(72f, -82f);
        titleRect.offsetMax = new Vector2(-72f, -18f);
        titleText.alignment = TextAnchor.MiddleCenter;

        bodyText = CreateText(panel.transform, "Body", 24, FontStyle.Normal, bodyColor);
        var bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(86f, -178f);
        bodyRect.offsetMax = new Vector2(-86f, -88f);
        bodyText.alignment = TextAnchor.MiddleCenter;
        bodyText.lineSpacing = 1f;

        CreateHintChip(panel.transform, new Vector2(-350f, 48f), "GRIP", "\uD578\uB4E4 \uC7A1\uAE30");
        CreateHintChip(panel.transform, new Vector2(0f, 48f), "STEER", "\uC88C\uC6B0 \uC870\uD5A5");
        CreateHintChip(panel.transform, new Vector2(350f, 48f), "TWIST", "\uBD80\uC2A4\uD2B8");
    }

    private void ApplyData()
    {
        if (activityData == null && manager != null)
            activityData = manager.activityData;

        if (titleText != null)
            titleText.text = activityData != null ? activityData.tutorialTitle : "\uC81C\uD2B8\uC2A4\uD0A4 \uC870\uC791 \uBC29\uBC95";

        if (bodyText != null)
        {
            bodyText.text = activityData != null
                ? activityData.tutorialBody
                : "\uCEE8\uD2B8\uB864\uB7EC \uC785\uB825\uC73C\uB85C \uC804\uC9C4\uD558\uACE0 \uC88C\uC6B0\uB85C \uC870\uD5A5\uD569\uB2C8\uB2E4.\n\uAC8C\uC774\uC9C0\uAC00 \uCC28\uBA74 \uC624\uB978\uC190 \uCEE8\uD2B8\uB864\uB7EC\uB97C \uBE44\uD2C0\uC5B4 \uBD80\uC2A4\uD2B8\uD558\uC138\uC694.\n\uAC8C\uC774\uD2B8\uB97C \uC21C\uC11C\uB300\uB85C \uD1B5\uACFC\uD558\uC138\uC694.";
        }
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
        text.resizeTextMinSize = Mathf.Max(16, fontSize - 10);
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

    private void CreateHintChip(Transform parent, Vector2 anchoredPosition, string label, string value)
    {
        var chip = new GameObject($"{label} Hint");
        chip.transform.SetParent(parent, false);

        var rect = chip.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(270f, 58f);

        var image = chip.AddComponent<Image>();
        image.sprite = CreateChipSprite(256, 64, 18);
        image.type = Image.Type.Sliced;

        var labelText = CreateText(chip.transform, "Text", 20, FontStyle.Bold, new Color(0.08f, 0.16f, 0.2f, 1f));
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.text = $"{label}  {value}";

        var labelRect = labelText.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 4f);
        labelRect.offsetMax = new Vector2(-12f, -4f);
    }

    private Sprite CreatePanelSprite(int width, int height, int radius)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "Generated JetSki Tutorial Panel";
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
                    color = Color.Lerp(panelColor, Color.white, topGlow * 0.12f);

                    bool border = !IsInsideRoundedRect(x, y, width, height, radius - 5);
                    if (border)
                        color = panelBorderColor;

                    if (y > height - 9 || y < 9)
                        color = accentColor;

                    if (y % 6 == 0 && y > 14 && y < height - 14)
                        color = Color.Lerp(color, Color.white, 0.15f);
                }
                else if (IsInsideRoundedRect(x - 8, y + 8, width, height, radius))
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

    private Sprite CreateChipSprite(int width, int height, int radius)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "Generated JetSki Tutorial Chip";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        var transparent = new Color(0f, 0f, 0f, 0f);
        var fill = new Color(0.85f, 0.97f, 1f, 0.88f);
        var border = new Color(0.18f, 0.82f, 1f, 0.95f);
        var pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color color = transparent;
                if (IsInsideRoundedRect(x, y, width, height, radius))
                {
                    color = fill;
                    if (!IsInsideRoundedRect(x, y, width, height, radius - 4))
                        color = border;
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
