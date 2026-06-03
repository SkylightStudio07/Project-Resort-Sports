using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 종료 결과 화면 UI입니다.
/// 10프레임 완료 시 플레이어 앞에 나타납니다.
///
/// [Unity 씬 설정]
/// 1. 빈 오브젝트에 Canvas (World Space) 추가 → 게임 시작 시 비활성화
/// 2. 최종 점수, 랭크, 재도전 버튼 배치
/// 3. 이 스크립트 부착 후 슬롯 연결
/// 4. ScoreManager.OnGameOver 이벤트로 자동 표시됨
/// </summary>
public class GameResultUI : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public GameObject      panel;          // 결과 화면 패널 (시작 시 비활성화)
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI rankText;

    [Header("버튼")]
    public Button retryButton;             // 재도전

    [Header("참조")]
    public ScoreManager scoreManager;
    public BallManager  ballManager;

    [Header("결과 화면 위치 설정")]
    [Tooltip("플레이어(XR Origin) Transform. 결과 화면이 플레이어 앞에 나타납니다.")]
    public Transform playerTransform;
    [Tooltip("플레이어 앞 거리 (m)")]
    public float distanceFromPlayer = 2f;

    [Tooltip("패널 높이 (눈높이 보정, m)")]
    public float panelHeight = 1.5f;

    // ── Unity 생명주기 ───────────────────────────────────────────────────

    private void Start()
    {
        panel.SetActive(false);

        scoreManager.OnGameOver += ShowResult;

        retryButton?.onClick.AddListener(OnRetry);
    }

    private void OnDestroy()
    {
        if (scoreManager != null)
            scoreManager.OnGameOver -= ShowResult;
    }

    // ── 결과 표시 ────────────────────────────────────────────────────────

    private void ShowResult(int finalScore, string rank)
    {
        panel.SetActive(true);

        finalScoreText?.SetText($"SCORE\n{finalScore}");

        // 랭크별 색상
        if (rankText != null)
        {
            rankText.SetText(rank);
            rankText.color = rank switch
            {
                "Gold"   => new Color(1f, 0.84f, 0f),
                "Silver" => new Color(0.75f, 0.75f, 0.75f),
                _        => new Color(0.8f, 0.5f, 0.2f)
            };
        }

        // 플레이어 앞에 패널 배치 (눈높이 보정)
        if (playerTransform != null)
        {
            Vector3 forward = playerTransform.forward;
            forward.y       = 0f;
            forward.Normalize();

            Vector3 pos = playerTransform.position + forward * distanceFromPlayer;
            pos.y      += panelHeight; // 눈높이만큼 올림
            panel.transform.position = pos;
            panel.transform.rotation = Quaternion.LookRotation(forward);
        }
    }

    // ── 버튼 콜백 ────────────────────────────────────────────────────────

    private void OnRetry()
    {
        panel.SetActive(false);
        ballManager.ResetGame();
    }
}
