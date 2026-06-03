using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

/// <summary>
/// XR Origin(XR Rig)에 붙여서 제트스키를 따라가되,
/// HTC 헤드셋의 고개 회전(TrackedPoseDriver)은 그대로 살림.
///
/// 핵심: 위치 + Yaw만 복사. Pitch/Roll은 복사 안 함(멀미 방지).
/// XR Origin은 제트스키의 자식이 아니어야 합니다.
/// </summary>
public class VRCameraFollow : MonoBehaviour
{
    [Tooltip("따라갈 제트스키 Transform")]
    public Transform target;

    [Tooltip("제트스키 로컬 기준 좌석 오프셋 (앞뒤/높이/좌우)")]
    public Vector3 seatOffset = new Vector3(0f, 0.5f, 0.3f);

    [Tooltip("실기기 VR에서 카메라가 낮게 느껴질 때 추가로 올리는 월드 Y 오프셋.")]
    public float vrHeightOffset = 0.2f;

    [Tooltip("제트스키의 Yaw(좌우 회전)를 XR Origin에 반영할지 여부.\n" +
             "true: 제트스키가 돌면 컨트롤러 '앞' 방향도 같이 돔(직관적).\n" +
             "false: 항상 월드 기준으로 고개를 돌려야 함.")]
    public bool followYaw = true;

    [Tooltip("위치 추적 부드러움. 0이면 즉시 이동, 클수록 부드럽게 따라옴 (초당 이동 속도 계수)")]
    [Range(0f, 30f)]
    public float positionLerpSpeed = 20f;

    void Awake()
    {
        // CharacterController: 제트스키 콜라이더와 충돌해서 XR Origin이 밀려남.
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
    }

    void Start()
    {
        // Awake()가 전부 끝난 뒤 비활성화 — 다른 컴포넌트가 Awake에서 enable했을 수 있음.
        var providers = GetComponentsInChildren<LocomotionProvider>(includeInactive: true);
        foreach (var lp in providers)
        {
            lp.enabled = false;
            Debug.Log($"[VRCameraFollow] disabled: {lp.GetType().Name} on {lp.gameObject.name}");
        }

        if (providers.Length == 0)
            Debug.LogWarning("[VRCameraFollow] LocomotionProvider를 하나도 못 찾았습니다. 계층 구조를 확인하세요.");
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 목표 위치: 제트스키의 로컬 seatOffset을 월드 좌표로 변환
        Vector3 targetPos = target.TransformPoint(seatOffset);
        targetPos += Vector3.up * vrHeightOffset;

        // 위치 추적 (LerpSpeed == 0이면 즉시)
        if (positionLerpSpeed > 0f)
            transform.position = Vector3.Lerp(transform.position, targetPos, positionLerpSpeed * Time.deltaTime);
        else
            transform.position = targetPos;

        // Yaw만 복사. Pitch(앞뒤 기울기)/Roll(좌우 기울기)은 복사하지 않음.
        // → 파도에 흔들려도 시야가 흔들리지 않아 멀미 방지.
        if (followYaw)
        {
            float targetYaw = target.eulerAngles.y;
            Quaternion targetRot = Quaternion.Euler(0f, targetYaw, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, positionLerpSpeed * Time.deltaTime > 0f ? positionLerpSpeed * Time.deltaTime : 1f);
        }
    }
}
