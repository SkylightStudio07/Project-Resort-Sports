using UnityEngine;

/// <summary>
/// 출구 앞 트리거 영역입니다.
/// 플레이어(Player 태그)가 들어오면 안내 UI를 띄우고, 나가면 닫습니다.
///
/// [Unity 씬 설정]
/// 1. 출구 앞에 빈 오브젝트 생성
/// 2. Box Collider 추가 → Is Trigger 체크
/// 3. 이 스크립트 부착 후 ExitPromptUI 연결
/// 4. XR Origin에 "Player" 태그 설정
/// </summary>
[RequireComponent(typeof(Collider))]
public class ExitDoorTrigger : MonoBehaviour
{
    [Tooltip("출구 안내 UI")]
    public ExitPromptUI exitPromptUI;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            exitPromptUI?.ShowPrompt();
            Debug.Log("[ExitDoorTrigger] 플레이어 출구 접근");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            exitPromptUI?.HidePrompt();
            Debug.Log("[ExitDoorTrigger] 플레이어 출구 이탈");
        }
    }
}
