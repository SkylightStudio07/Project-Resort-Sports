using TMPro;
using System.Collections;
using UnityEngine;

/// <summary>
/// 볼이 복귀해 투구 준비가 됐을 때 표시하는 간단한 인디케이터입니다.
/// 볼 스폰 포인트 근처 World Space Canvas에 배치합니다.
/// </summary>
public class ReadyIndicatorUI : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public TextMeshProUGUI readyText;
    public CanvasGroup     canvasGroup;

    [Header("참조")]
    public BallManager ballManager;

    [Header("연출")]
    public float fadeDuration = 0.3f;
    public float displayDuration = 1.5f;

    // ── Unity 생명주기 ───────────────────────────────────────────────────

    private void Start()
    {
        ballManager.OnBallReady += ShowReady;
        canvasGroup.alpha        = 0f;

        if (readyText != null)
            readyText.SetText("READY");
    }

    private void OnDestroy()
    {
        if (ballManager != null)
            ballManager.OnBallReady -= ShowReady;
    }

    // ── 표시 ────────────────────────────────────────────────────────────

    private void ShowReady()
    {
        StartCoroutine(FadeInOut());
    }

    private IEnumerator FadeInOut()
    {
        // 페이드 인
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = t / fadeDuration;
            yield return null;
        }
        canvasGroup.alpha = 1f;

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
