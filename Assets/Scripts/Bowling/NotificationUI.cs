using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Strike / Spare / Gutter 알림 UI입니다 (이미지 방식).
// 레인 중앙 상단 World Space Canvas에 배치합니다.
// 스프라이트가 확대되며 등장(오버슈트)했다가 페이드 아웃됩니다.
public class NotificationUI : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public Image       notificationImage;
    public CanvasGroup canvasGroup;

    [Header("알림 스프라이트")]
    public Sprite strikeSprite;
    public Sprite spareSprite;
    public Sprite gutterSprite;

    [Header("연출 설정")]
    [Tooltip("이미지가 표시되는 시간 (초)")]
    public float displayDuration = 2f;

    [Tooltip("등장(확대) 시간 (초)")]
    public float popInDuration = 0.25f;

    [Tooltip("오버슈트 후 정상 크기로 돌아오는 시간 (초)")]
    public float settleDuration = 0.12f;

    [Tooltip("페이드 아웃 시간 (초)")]
    public float fadeOutDuration = 0.3f;

    [Tooltip("등장 시작 크기 배율")]
    public float startScale = 0.3f;

    [Tooltip("오버슈트 최대 크기 배율")]
    public float overshootScale = 1.15f;

    [Header("볼링 이벤트 참조")]
    public ScoreManager scoreManager;
    public BallManager  ballManager;

    // ── 내부 상태 ────────────────────────────────────────────────────────
    private Coroutine _currentCoroutine;
    private Vector3 _initScale;

    // ── Unity 생명주기 ───────────────────────────────────────────────────

    private void Start()
    {
        scoreManager.OnStrike    += HandleStrike;
        scoreManager.OnSpare     += HandleSpare;
        ballManager.OnGutterBall += HandleGutter;

        canvasGroup.alpha = 0f;
        _initScale = transform.localScale;
    }

    private void OnDestroy()
    {
        if (scoreManager != null)
        {
            scoreManager.OnStrike -= HandleStrike;
            scoreManager.OnSpare  -= HandleSpare;
        }
        if (ballManager != null)
            ballManager.OnGutterBall -= HandleGutter;
    }

    // ── 이벤트 핸들러 ────────────────────────────────────────────────────

    private void HandleStrike() => ShowNotification(strikeSprite);
    private void HandleSpare()  => ShowNotification(spareSprite);
    private void HandleGutter() => ShowNotification(gutterSprite);

    // ── 알림 표시 ────────────────────────────────────────────────────────

    public void ShowNotification(Sprite sprite)
    {
        if (sprite == null) return;

        if (_currentCoroutine != null)
            StopCoroutine(_currentCoroutine);

        notificationImage.sprite = sprite;
        _currentCoroutine = StartCoroutine(PlayNotification());
    }

    private IEnumerator PlayNotification()
    {
        // ── 등장: startScale → overshootScale (확대) ────────────────────
        float t = 0f;
        transform.localScale = _initScale * startScale;
        canvasGroup.alpha    = 0f;

        while (t < popInDuration)
        {
            t += Time.deltaTime;
            float p              = t / popInDuration;
            // EaseOut 곡선 (빠르게 커졌다 느려짐)
            float eased          = 1f - Mathf.Pow(1f - p, 3f);
            canvasGroup.alpha    = p;
            transform.localScale = _initScale * Mathf.Lerp(startScale, overshootScale, eased);
            yield return null;
        }

        // ── 정착: overshootScale → 1.0 (탄력감) ─────────────────────────
        t = 0f;
        while (t < settleDuration)
        {
            t += Time.deltaTime;
            transform.localScale = _initScale * Mathf.Lerp(overshootScale, 1f, t / settleDuration);
            yield return null;
        }

        canvasGroup.alpha    = 1f;
        transform.localScale = _initScale;

        // ── 유지 ────────────────────────────────────────────────────────
        yield return new WaitForSeconds(displayDuration);

        // ── 퇴장: 페이드 아웃 ───────────────────────────────────────────
        t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = 1f - (t / fadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
}
