using UnityEngine;

/// <summary>
/// 볼링 스코어를 계산합니다.
/// 10프레임 진행, 스트라이크/스페어 보너스를 포함한 정식 규칙을 구현합니다.
/// 결과는 현재 Debug.Log로 출력합니다 (UI 연결은 다음 단계).
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // 최대 21구 (10번째 프레임 보너스 포함)
    private int[] _throws    = new int[21];
    private int   _throwIdx  = 0;

    private int  _currentFrame   = 0; // 0~9
    private int  _throwInFrame   = 0; // 현재 프레임 내 몇 번째 투구

    public PinManager pinManager;

    // ── 투구 기록 ──────────────────────────────────────────────────────

    /// <summary>PinManager에서 핀 집계 후 호출합니다.</summary>
    public void RecordThrow(int pinsKnocked)
    {
        if (_throwIdx >= 21 || _currentFrame >= 10)
        {
            Debug.Log("[ScoreManager] 게임이 이미 종료되었습니다.");
            return;
        }

        _throws[_throwIdx++] = pinsKnocked;

        Debug.Log($"[ScoreManager] {_currentFrame + 1}프레임 {_throwInFrame + 1}구: {pinsKnocked}핀  현재 총점: {CalculateTotalScore()}");

        AdvanceFrame(pinsKnocked);
    }

    // ── 프레임 진행 ────────────────────────────────────────────────────

    private void AdvanceFrame(int pinsKnocked)
    {
        if (_currentFrame < 9) // 1~9번째 프레임
        {
            if (_throwInFrame == 0 && pinsKnocked == 10) // 스트라이크
            {
                Debug.Log($"[ScoreManager] 스트라이크!");
                _currentFrame++;
                _throwInFrame = 0;
                pinManager.ResetAllPins(); // 다음 프레임 핀 리셋
            }
            else if (_throwInFrame == 1) // 2구 완료
            {
                if (pinsKnocked + _throws[_throwIdx - 2] == 10)
                    Debug.Log($"[ScoreManager] 스페어!");

                _currentFrame++;
                _throwInFrame = 0;
                pinManager.ResetAllPins(); // 다음 프레임 핀 리셋
            }
            else // 1구 완료, 2구 준비
            {
                _throwInFrame = 1;
                pinManager.ResetCountForSecondThrow();
            }
        }
        else // 10번째 프레임
        {
            _throwInFrame++;

            bool firstStrike  = _throws[_throwIdx - _throwInFrame] == 10;
            bool spare        = _throwInFrame == 2 &&
                                _throws[_throwIdx - 2] + _throws[_throwIdx - 1] == 10;

            // 10번째 프레임 종료 조건
            if (_throwInFrame == 2 && !firstStrike && !spare)
            {
                // 스트라이크도 스페어도 아니면 2구로 종료
                EndGame();
            }
            else if (_throwInFrame == 3)
            {
                EndGame();
            }
        }
    }

    private void EndGame()
    {
        int finalScore = CalculateTotalScore();
        Debug.Log($"[ScoreManager] 게임 종료! 최종 점수: {finalScore}  랭크: {GetRank(finalScore)}");
        _currentFrame = 10; // 종료 상태
    }

    // ── 스코어 계산 ────────────────────────────────────────────────────

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

    // ── 게임 초기화 ────────────────────────────────────────────────────

    public void ResetGame()
    {
        System.Array.Clear(_throws, 0, _throws.Length);
        _throwIdx    = 0;
        _currentFrame = 0;
        _throwInFrame = 0;
        pinManager.ResetAllPins();
        Debug.Log("[ScoreManager] 게임 초기화");
    }
}
