using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Strike / Spare / Gutter 알림 UI입니다.
/// 레인 중앙 상단 World Space Canvas에 배치합니다.
/// 텍스트가 크게 나타났다가 페이드 아웃됩니다.
///
/// [Unity 씬 설정]
/// 1. 레인 위에 빈 오브젝트 생성 → Canvas (World Space) 추가
/// 2. 캔버스 아래 TextMeshProUGUI 하나 배치
/// 3. 캔버스에 CanvasGroup 컴포넌트 추가
/// 4. 이 스크립트 부착 후 슬롯 연결
/// </summary>
public class NotificationUI : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public TextMeshProUGUI notificationText;
    public CanvasGroup     canvasGroup;

    [Header("연출 설정")]
    [Tooltip("텍스트가 표시되는 시간 (초)")]
    public float displayDuration = 2f;

    [Tooltip("페이드 인/아웃 시간 (초)")]
    public float fadeDuration    = 0.3f;

    [Header("볼링 이벤트 참조")]
    public ScoreManager scoreManager;
    public BallManager  ballManager;

    // ── 내부 상태 ────────────────────────────────────────────────────────
    private Coroutine _currentCoroutine;

    // ── Unity 생명주기 ───────────────────────────────────────────────────

    private void Start()
    {
        scoreManager.OnStrike    += () => ShowNotification("STRIKE!", new Color(1f, 0.85f, 0f));
        scoreManager.OnSpare     += () => ShowNotification("SPARE!",  new Color(0.4f, 0.9f, 1f));
        ballManager.OnGutterBall += () => ShowNotification("GUTTER",  new Color(1f, 0.4f, 0.4f));

        canvasGroup.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (scoreManager != null)
        {
            scoreManager.OnStrike -= () => ShowNotification("STRIKE!", Color.yellow);
            scoreManager.OnSpare  -= () => ShowNotification("SPARE!",  Color.cyan);
        }
        if (ballManager != null)
            ballManager.OnGutterBall -= () => ShowNotification("GUTTER", Color.red);
    }

    // ── 알림 표시 ────────────────────────────────────────────────────────

    public void ShowNotification(string message, Color color)
    {
        if (_currentCoroutine != null)
            StopCoroutine(_currentCoroutine);

        notificationText.text  = message;
        notificationText.color = color;

        _currentCoroutine = StartCoroutine(PlayNotification());
    }

    private IEnumerator PlayNotification()
    {
        // 페이드 인 + 스케일 펀치
        float t = 0f;
        transform.localScale = Vector3.one * 0.5f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float progress      = t / fadeDuration;
            canvasGroup.alpha   = progress;
            transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.1f, progress);
            yield return null;
        }

        // 스케일 살짝 줄이기 (탄력감)
        t = 0f;
        float punchDuration = 0.1f;
        while (t < punchDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.one * Mathf.Lerp(1.1f, 1f, t / punchDuration);
            yield return null;
        }

        canvasGroup.alpha    = 1f;
        transform.localScale = Vector3.one;

        // 표시 유지
        yield return new WaitForSeconds(displayDuration);

        // 페이드 아웃
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = 1f - (t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
}
