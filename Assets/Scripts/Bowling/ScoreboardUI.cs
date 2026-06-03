using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 볼링 점수 전광판 UI입니다.
/// World Space Canvas에 배치하고 ScoreManager.OnScoreUpdated를 구독합니다.
///
/// [Inspector 연결 순서]
/// 1. ScoreManager 슬롯 연결
/// 2. Frame Cells 배열 크기 10 설정 후 Frame_1 ~ Frame_10 순서대로 연결
/// 3. Total Score Text 슬롯 연결
/// </summary>
public class ScoreboardUI : MonoBehaviour
{
    [Serializable]
    public class FrameCell
    {
        [Header("배경 오브젝트 (활성/비활성 제어)")]
        public GameObject          throw1Background;   // 1구 배경
        public GameObject          throw2Background;   // 2구 배경

        [Header("텍스트")]
        public TextMeshProUGUI     throw1Text;         // 1구 결과 (X, 숫자, -)
        public TextMeshProUGUI     throw2Text;         // 2구 결과 (/, 숫자, -)
        public TextMeshProUGUI     throw3Text;         // 3구 (10번째 프레임만)
        public TextMeshProUGUI     cumulativeScoreText;// 누적 점수

        [Header("현재 프레임 강조")]
        public Image               highlightImage;     // 현재 프레임 테두리
    }

    [Header("프레임 셀 (Frame_1 ~ Frame_10 순서로 연결)")]
    public FrameCell[] frameCells = new FrameCell[10];

    [Header("총점 표시")]
    public TextMeshProUGUI totalScoreText;

    [Header("현재 프레임 / 투구 정보")]
    public TextMeshProUGUI currentFrameText;
    public TextMeshProUGUI currentThrowText;

    [Header("참조")]
    public ScoreManager scoreManager;

    // ── Unity 생명주기 ───────────────────────────────────────────────────

    private void Start()
    {
        scoreManager.OnScoreUpdated += RefreshBoard;
        RefreshBoard(); // 초기 상태 (모든 칸 비활성화)
    }

    private void OnDestroy()
    {
        if (scoreManager != null)
            scoreManager.OnScoreUpdated -= RefreshBoard;
    }

    // ── 전광판 갱신 ──────────────────────────────────────────────────────

    private void RefreshBoard()
    {
        int[] cumulatives = scoreManager.GetCumulativeScores();

        for (int i = 0; i < 10; i++)
        {
            if (i >= frameCells.Length || frameCells[i] == null) continue;

            var (t1, t2, t3) = scoreManager.GetFrameThrows(i);

            // ── 투구 배경 활성/비활성 제어 ──────────────────────────────
            bool hasThrow1 = !string.IsNullOrEmpty(t1);
            bool hasThrow2 = !string.IsNullOrEmpty(t2);

            SetActive(frameCells[i].throw1Background, hasThrow1);
            SetActive(frameCells[i].throw2Background, hasThrow2);

            // ── 투구 텍스트 설정 ─────────────────────────────────────────
            SetText(frameCells[i].throw1Text, t1);
            SetText(frameCells[i].throw2Text, t2);
            SetText(frameCells[i].throw3Text, t3); // 10번째 외 프레임은 ""

            // ── 누적 점수: -1이면 보너스 미확정 → 빈 칸 ─────────────────
            string scoreStr = cumulatives[i] >= 0 ? cumulatives[i].ToString() : "";
            SetText(frameCells[i].cumulativeScoreText, scoreStr);

            // ── 현재 프레임 강조 ─────────────────────────────────────────
            bool isCurrent = (i == scoreManager.CurrentFrame && !scoreManager.IsGameOver);
            if (frameCells[i].highlightImage != null)
                frameCells[i].highlightImage.enabled = isCurrent;
        }

        // ── 총점 ────────────────────────────────────────────────────────
        SetText(totalScoreText, scoreManager.CalculateTotalScore().ToString());

        // ── 현재 프레임 / 투구 정보 ─────────────────────────────────────
        if (!scoreManager.IsGameOver)
        {
            SetText(currentFrameText, $"FRAME {scoreManager.CurrentFrame + 1}");
            SetText(currentThrowText, $"THROW {scoreManager.ThrowInFrame + 1}");
        }
        else
        {
            SetText(currentFrameText, "GAME OVER");
            SetText(currentThrowText, "");
        }
    }

    // ── 헬퍼 ────────────────────────────────────────────────────────────

    private void SetText(TextMeshProUGUI tmp, string value)
    {
        if (tmp != null) tmp.SetText(value);
    }

    private void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }
}
