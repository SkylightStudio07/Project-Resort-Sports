using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 출구 안내 UI입니다.
/// "리조트로 나가시겠습니까?" 패널과 YES 버튼을 관리합니다.
/// YES 버튼을 누르면 플레이어(XR Origin)를 리조트 복귀 지점으로 이동시킵니다.
///
/// [Unity 씬 설정]
/// 1. 출구 앞에 World Space Canvas 배치 → 시작 시 비활성화
/// 2. YES 버튼 + 안내 텍스트 배치
/// 3. 이 스크립트 부착 후 슬롯 연결
/// </summary>
public class ExitPromptUI : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [Tooltip("안내 패널 (시작 시 비활성화)")]
    public GameObject promptPanel;

    [Tooltip("YES 버튼")]
    public Button yesButton;

    [Header("플레이어 / 복귀 지점")]
    [Tooltip("플레이어 (XR Origin) Transform")]
    public Transform xrOrigin;

    [Tooltip("리조트 복귀 지점")]
    public Transform resortReturnPoint;

    private void Start()
    {
        if (promptPanel != null) promptPanel.SetActive(false);

        if (yesButton != null)
            yesButton.onClick.AddListener(OnYesClicked);
    }

    // ── 외부 호출 (ExitDoorTrigger에서) ──────────────────────────────────

    /// <summary>출구 영역 진입 시 안내 패널을 엽니다.</summary>
    public void ShowPrompt()
    {
        if (promptPanel != null) promptPanel.SetActive(true);
    }

    /// <summary>출구 영역 이탈 시 안내 패널을 닫습니다.</summary>
    public void HidePrompt()
    {
        if (promptPanel != null) promptPanel.SetActive(false);
    }

    // ── 버튼 콜백 ────────────────────────────────────────────────────────

    private void OnYesClicked()
    {
        TeleportToResort();
        HidePrompt();
    }

    private void TeleportToResort()
    {
        if (xrOrigin == null || resortReturnPoint == null)
        {
            Debug.LogWarning("[ExitPromptUI] XR Origin 또는 복귀 지점이 연결되지 않았습니다.");
            return;
        }

        xrOrigin.SetPositionAndRotation(
            resortReturnPoint.position,
            resortReturnPoint.rotation);

        Debug.Log("[ExitPromptUI] 리조트로 복귀");
    }
}
