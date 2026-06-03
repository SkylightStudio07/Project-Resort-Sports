using System;
using UnityEngine;

/// <summary>
/// 볼링 스코어를 계산합니다.
/// 핀 리셋 / 게임 종료 타이밍은 이벤트로 BallManager에 전달합니다.
/// Strike / Spare / 스코어 업데이트 이벤트는 UI가 구독합니다.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // ── 이벤트 ──────────────────────────────────────────────────────────
    public event Action           OnPinResetNeeded;  // 핀 전체 리셋 필요
    public event Action<int, string> OnGameOver;     // (최종점수, 랭크)
    public event Action           OnStrike;          // 스트라이크 발생
    public event Action           OnSpare;           // 스페어 발생
    public event Action           OnScoreUpdated;    // 투구 기록 후 전광판 갱신용

    // ── 내부 데이터 ──────────────────────────────────────────────────────
    private int[] _throws      = new int[21];
    private int   _throwIdx    = 0;
    private int   _currentFrame = 0; // 0~9
    private int   _throwInFrame = 0;

    // ── 공개 프로퍼티 ────────────────────────────────────────────────────
    public int  CurrentFrame  => _currentFrame;
    public int  ThrowInFrame  => _throwInFrame;
    public bool IsGameOver    => _currentFrame >= 10;

    // ── 투구 기록 ────────────────────────────────────────────────────────

    public void RecordThrow(int pinsKnocked)
    {
        if (IsGameOver) return;

        _throws[_throwIdx++] = pinsKnocked;
        Debug.Log($"[ScoreManager] {_currentFrame + 1}프레임 {_throwInFrame + 1}구: " +
                  $"{pinsKnocked}핀  현재 총점: {CalculateTotalScore()}");

        AdvanceFrame(pinsKnocked);
        OnScoreUpdated?.Invoke();
    }

    // ── 프레임 진행 ──────────────────────────────────────────────────────

    private void AdvanceFrame(int pinsKnocked)
    {
        if (_currentFrame < 9)
        {
            if (_throwInFrame == 0 && pinsKnocked == 10)
            {
                OnStrike?.Invoke();
                _currentFrame++;
                _throwInFrame = 0;
                OnPinResetNeeded?.Invoke();
            }
            else if (_throwInFrame == 1)
            {
                if (_throws[_throwIdx - 2] + pinsKnocked == 10)
                    OnSpare?.Invoke();

                _currentFrame++;
                _throwInFrame = 0;
                OnPinResetNeeded?.Invoke();
            }
            else
            {
                _throwInFrame = 1;
            }
        }
        else // 10번째 프레임
        {
            _throwInFrame++;

            bool firstStrike = (_throws[_throwIdx - _throwInFrame] == 10);
            bool spare       = (_throwInFrame == 2 &&
                                _throws[_throwIdx - 2] + _throws[_throwIdx - 1] == 10);

            if (_throwInFrame == 0 && firstStrike)      OnStrike?.Invoke();
            if (_throwInFrame == 1 && spare)             OnSpare?.Invoke();

            if      (_throwInFrame == 1 && firstStrike)  OnPinResetNeeded?.Invoke();
            else if (_throwInFrame == 2 && spare)         OnPinResetNeeded?.Invoke();
            else if (_throwInFrame == 2 && !firstStrike && !spare) EndGame();
            else if (_throwInFrame == 3)                  EndGame();
        }
    }

    private void EndGame()
    {
        int    finalScore = CalculateTotalScore();
        string rank       = GetRank(finalScore);
        Debug.Log($"[ScoreManager] 게임 종료! 최종 점수: {finalScore}  랭크: {rank}");
        _currentFrame = 10;
        OnGameOver?.Invoke(finalScore, rank);
        OnScoreUpdated?.Invoke();
    }

    // ── 스코어 계산 ──────────────────────────────────────────────────────

    public int CalculateTotalScore()
    {
        int score   = 0;
        int rollIdx = 0;

        for (int frame = 0; frame < 10 && rollIdx < _throwIdx; frame++)
        {
            if (_throws[rollIdx] == 10)
            {
                score   += 10 + SafeGet(rollIdx + 1) + SafeGet(rollIdx + 2);
                rollIdx++;
            }
            else if (_throws[rollIdx] + SafeGet(rollIdx + 1) == 10)
            {
                score   += 10 + SafeGet(rollIdx + 2);
                rollIdx += 2;
            }
            else
            {
                score   += _throws[rollIdx] + SafeGet(rollIdx + 1);
                rollIdx += 2;
            }
        }
        return score;
    }

    /// <summary>
    /// 프레임별 누적 점수 배열을 반환합니다 (전광판용).
    /// 보너스 투구가 아직 없어서 계산 불가한 프레임은 -1을 반환합니다.
    /// </summary>
    public int[] GetCumulativeScores()
    {
        var result  = new int[10];
        int rollIdx = 0;
        int running = 0;

        for (int frame = 0; frame < 10; frame++)
        {
            result[frame] = -1; // 기본값: 아직 계산 불가

            if (rollIdx >= _throwIdx) break;

            if (_throws[rollIdx] == 10 && frame < 9) // 스트라이크 (9프레임까지)
            {
                if (rollIdx + 2 < _throwIdx)
                {
                    running       += 10 + SafeGet(rollIdx + 1) + SafeGet(rollIdx + 2);
                    result[frame]  = running;
                }
                rollIdx++;
            }
            else if (rollIdx + 1 < _throwIdx && _throws[rollIdx] + _throws[rollIdx + 1] == 10 && frame < 9) // 스페어
            {
                if (rollIdx + 2 < _throwIdx)
                {
                    running       += 10 + SafeGet(rollIdx + 2);
                    result[frame]  = running;
                }
                rollIdx += 2;
            }
            else if (rollIdx + 1 < _throwIdx) // 오픈 프레임
            {
                running       += _throws[rollIdx] + _throws[rollIdx + 1];
                result[frame]  = running;
                rollIdx       += 2;
            }
        }

        return result;
    }

    /// <summary>
    /// 특정 프레임의 투구 표시 문자열을 반환합니다 (전광판용).
    /// 예: 스트라이크="X", 스페어="/", 거터="-"
    /// </summary>
    public (string t1, string t2, string t3) GetFrameThrows(int frameIndex)
    {
        int rollIdx = GetFrameStartIdx(frameIndex);

        if (rollIdx >= _throwIdx) return ("", "", "");

        if (frameIndex < 9)
        {
            int t1v = rollIdx < _throwIdx     ? _throws[rollIdx]     : -1;
            int t2v = rollIdx + 1 < _throwIdx ? _throws[rollIdx + 1] : -1;

            string s1 = FormatThrow(t1v, -1);
            string s2 = t2v < 0 ? "" : (t1v + t2v == 10 ? "/" : FormatThrow(t2v, -1));
            return (s1, s2, "");
        }
        else // 10번째 프레임
        {
            int t1v = rollIdx     < _throwIdx ? _throws[rollIdx]     : -1;
            int t2v = rollIdx + 1 < _throwIdx ? _throws[rollIdx + 1] : -1;
            int t3v = rollIdx + 2 < _throwIdx ? _throws[rollIdx + 2] : -1;

            string s1 = FormatThrow(t1v, -1);
            string s2 = "";
            string s3 = "";

            if (t2v >= 0)
                s2 = (t1v == 10 && t2v == 10) ? "X"
                   : (t1v != 10 && t1v + t2v == 10) ? "/"
                   : FormatThrow(t2v, -1);

            if (t3v >= 0)
                s3 = FormatThrow(t3v, -1);

            return (s1, s2, s3);
        }
    }

    // ── 내부 헬퍼 ────────────────────────────────────────────────────────

    private int GetFrameStartIdx(int frameIndex)
    {
        int idx = 0;
        for (int f = 0; f < frameIndex && f < 9; f++)
        {
            if (idx < _throwIdx && _throws[idx] == 10) idx++;
            else                                        idx += 2;
        }
        return idx;
    }

    private string FormatThrow(int val, int prev)
    {
        if (val < 0)  return "";
        if (val == 0) return "-";
        if (val == 10) return "X";
        return val.ToString();
    }

    private int SafeGet(int idx)
        => (idx >= 0 && idx < _throwIdx) ? _throws[idx] : 0;

    private string GetRank(int score)
    {
        if (score >= 200) return "Gold";
        if (score >= 120) return "Silver";
        return "Bronze";
    }

    public void ResetGame()
    {
        Array.Clear(_throws, 0, _throws.Length);
        _throwIdx     = 0;
        _currentFrame = 0;
        _throwInFrame = 0;
        OnScoreUpdated?.Invoke();
        Debug.Log("[ScoreManager] 게임 초기화");
    }
}
