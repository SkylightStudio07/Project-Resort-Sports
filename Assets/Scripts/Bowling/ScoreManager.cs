using System;
using UnityEngine;

/// <summary>
/// 볼링 스코어를 계산합니다.
/// 핀 리셋 / 게임 종료 타이밍은 이벤트로 BallManager에 전달합니다.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // ── 이벤트 ──────────────────────────────────────────────────────────
    /// <summary>프레임이 끝나 핀 전체 리셋이 필요할 때 발생합니다.</summary>
    public event Action OnPinResetNeeded;

    /// <summary>10프레임이 모두 끝났을 때 발생합니다.</summary>
    public event Action<int, string> OnGameOver; // (최종점수, 랭크)

    // ── 내부 데이터 ──────────────────────────────────────────────────────
    private int[] _throws     = new int[21];
    private int   _throwIdx   = 0;
    private int   _currentFrame  = 0; // 0~9
    private int   _throwInFrame  = 0; // 현재 프레임 내 몇 번째 투구

    // ── 공개 프로퍼티 ────────────────────────────────────────────────────
    public int  CurrentFrame   => _currentFrame;
    public int  ThrowInFrame   => _throwInFrame;
    public bool IsGameOver     => _currentFrame >= 10;

    // ── 투구 기록 ────────────────────────────────────────────────────────

    /// <summary>BallManager에서 핀 집계 후 호출합니다.</summary>
    public void RecordThrow(int pinsKnocked)
    {
        if (IsGameOver)
        {
            Debug.Log("[ScoreManager] 게임이 이미 종료되었습니다.");
            return;
        }

        _throws[_throwIdx++] = pinsKnocked;

        Debug.Log($"[ScoreManager] {_currentFrame + 1}프레임 {_throwInFrame + 1}구: " +
                  $"{pinsKnocked}핀  현재 총점: {CalculateTotalScore()}");

        AdvanceFrame(pinsKnocked);
    }

    // ── 프레임 진행 ──────────────────────────────────────────────────────

    private void AdvanceFrame(int pinsKnocked)
    {
        if (_currentFrame < 9) // 1~9번째 프레임
        {
            if (_throwInFrame == 0 && pinsKnocked == 10) // 스트라이크
            {
                Debug.Log("[ScoreManager] 스트라이크!");
                _currentFrame++;
                _throwInFrame = 0;
                OnPinResetNeeded?.Invoke(); // 핀 리셋 요청
            }
            else if (_throwInFrame == 1) // 2구 완료
            {
                bool isSpare = (_throws[_throwIdx - 2] + pinsKnocked == 10);
                if (isSpare) Debug.Log("[ScoreManager] 스페어!");

                _currentFrame++;
                _throwInFrame = 0;
                OnPinResetNeeded?.Invoke(); // 핀 리셋 요청
            }
            else // 1구 완료 → 2구 준비
            {
                _throwInFrame = 1;
                // 핀은 그대로, BallManager가 SnapshotForSecondThrow 호출
            }
        }
        else // 10번째 프레임
        {
            _throwInFrame++;

            bool firstStrike = (_throws[_throwIdx - _throwInFrame] == 10);
            bool spare       = (_throwInFrame == 2 &&
                                _throws[_throwIdx - 2] + _throws[_throwIdx - 1] == 10);

            if (_throwInFrame == 1 && firstStrike)
            {
                OnPinResetNeeded?.Invoke(); // 스트라이크 후 핀 리셋
            }
            else if (_throwInFrame == 2 && spare)
            {
                OnPinResetNeeded?.Invoke(); // 스페어 후 핀 리셋
            }
            else if (_throwInFrame == 2 && !firstStrike && !spare)
            {
                EndGame(); // 2구로 종료
            }
            else if (_throwInFrame == 3)
            {
                EndGame(); // 3구로 종료
            }
        }
    }

    private void EndGame()
    {
        int    finalScore = CalculateTotalScore();
        string rank       = GetRank(finalScore);
        Debug.Log($"[ScoreManager] 게임 종료! 최종 점수: {finalScore}  랭크: {rank}");
        _currentFrame = 10;
        OnGameOver?.Invoke(finalScore, rank);
    }

    // ── 스코어 계산 ──────────────────────────────────────────────────────

    public int CalculateTotalScore()
    {
        int score   = 0;
        int rollIdx = 0;

        for (int frame = 0; frame < 10 && rollIdx < _throwIdx; frame++)
        {
            if (_throws[rollIdx] == 10) // 스트라이크
            {
                score   += 10 + SafeGet(rollIdx + 1) + SafeGet(rollIdx + 2);
                rollIdx += 1;
            }
            else if (_throws[rollIdx] + SafeGet(rollIdx + 1) == 10) // 스페어
            {
                score   += 10 + SafeGet(rollIdx + 2);
                rollIdx += 2;
            }
            else // 오픈 프레임
            {
                score   += _throws[rollIdx] + SafeGet(rollIdx + 1);
                rollIdx += 2;
            }
        }

        return score;
    }

    private int SafeGet(int idx)
        => (idx >= 0 && idx < _throwIdx) ? _throws[idx] : 0;

    private string GetRank(int score)
    {
        if (score >= 200) return "Gold 🥇";
        if (score >= 120) return "Silver 🥈";
        return "Bronze 🥉";
    }

    public void ResetGame()
    {
        Array.Clear(_throws, 0, _throws.Length);
        _throwIdx    = 0;
        _currentFrame = 0;
        _throwInFrame = 0;
        Debug.Log("[ScoreManager] 게임 초기화");
    }
}
