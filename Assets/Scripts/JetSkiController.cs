using UnityEngine;
using UnityEngine.InputSystem;
using StylizedWater2;
using UnityEngine.XR;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class JetSkiController : MonoBehaviour
{
    [Header("물 오브젝트")]
    public WaterObject waterObject;

    // 
    [Header("부력")]
    // 4코너 샘플링으로 간단한 부력 시뮬레이션. 높을수록 물에 잠긴 부분이 더 강하게 떠오름
    public float buoyancyForce = 20f;
    // 부력 감쇠. 높을수록 물에 잠긴 부분의 움직임이 더 빠르게 감쇠
    public float buoyancyDamping = 3f;
    [Tooltip("상승 시 댐핑 배율(0~1). 0이면 크레스트에서 분리가 잘 되지만 탱탱볼처럼 튐. 보통 0.2~0.5.")]
    [Range(0f, 1f)]
    public float ascentDampingMul = 0.3f;
    // 롤링 안정화 강도. 높을수록 빠르게 수평 유지
    public float rollStrength = 1f;

    [Header("착지 댐핑")]
    [Tooltip("착지 직후 반동 억제 시간(초). 클수록 착지가 부드러워짐. 보통 0.15~0.3.")]
    public float landingDampDuration = 0.2f;
    [Tooltip("착지 감지 최소 하강 속도(m/s). 이보다 빠르게 입수해야 착지 댐핑 발동.")]
    public float landingVelThreshold = 2.5f;

    [Header("이동")]

    //
    public float throttle = 15f;
    [Tooltip("조향 토크 크기. 클수록 빠르게 회전.")]
    public float turnSpeed = 180f;
    [Tooltip("회전 감쇠. 낮을수록 잘 돌고 드리프트 느낌. 높으면 즉시 멈춤. 보통 1~5.")]
    public float angularDrag = 2f;
    [Tooltip("수평 드래그(공기/수면 마찰). 수직축에는 적용 안 됨 → 점프 가능.")]
    public float linearDrag = 2f;
    [Tooltip("파도 경사면을 따라 앞으로 미는 힘. 0이면 비활성. 크레스트 넘을 때 살짝 띄움.")]
    public float waveSlopeAssist = 8f;

    [Header("VR Input")]
    [Tooltip("XRI Right Hand/Activate Value — 오른쪽 트리거")]

    // throttle: 오른쪽 트리거, steer: 오른쪽 트랙패드/스틱 (X축 조향)
    public InputActionReference throttleAction;
    [Tooltip("XRI Right Hand/Thumbstick — 오른쪽 트랙패드/스틱 (X축 조향)")]
    public InputActionReference steerAction;

    [Header("Boost (optional)")]
    [Tooltip("같은 오브젝트에 붙은 JetSkiBoost. 비워두면 자동 탐색, 없어도 동작.")]
    public JetSkiBoost boost;

    [Header("Events")]
    public UnityEvent<float> onWaterLanding = new UnityEvent<float>();

    private Rigidbody rb;
    private float landingDampTimer;
    private bool wasSubmerged;

    // 4코너 샘플 포인트 (로컬 스페이스)
    private static readonly Vector3[] sampleOffsets =
    {
        new Vector3( 0.5f, 0f,  1f),
        new Vector3(-0.5f, 0f,  1f),
        new Vector3( 0.5f, 0f, -1f),
        new Vector3(-0.5f, 0f, -1f),
    };

    void OnEnable()
    {
        throttleAction?.action.Enable();
        steerAction?.action.Enable();
    }

    void OnDisable()
    {
        throttleAction?.action.Disable();
        steerAction?.action.Disable();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // 전축 드래그는 0 — 수직축까지 깎이면 점프가 죽음. 수평 드래그는 FixedUpdate에서 수동 적용.
        rb.drag = 0f;
        rb.angularDrag = angularDrag;
        if (boost == null) boost = GetComponent<JetSkiBoost>();
    }

    void FixedUpdate()
    {
        ApplyBuoyancy();
        ApplyHorizontalDrag();
        ApplyMovement();
    }

    void ApplyBuoyancy()
    {
        if (waterObject == null) return;

        Vector3 avgNormal = Vector3.zero;
        int submergedCount = 0;

        bool isLandingDamp = landingDampTimer > 0f;

        foreach (var offset in sampleOffsets)
        {
            Vector3 worldPoint = transform.TransformPoint(offset);
            float waveHeight = Buoyancy.SampleWaves(worldPoint, waterObject, rollStrength, false, out Vector3 normal);

            float depth = waveHeight - worldPoint.y;
            if (depth <= 0f) continue;

            float velY = rb.GetPointVelocity(worldPoint).y;
            // 비대칭 댐핑: 하강 또는 착지 직후엔 풀 댐핑, 파도 상승 시엔 약하게(크레스트 분리용)
            float dampMul = (velY < 0f || isLandingDamp) ? 1f : ascentDampingMul;
            float force = depth * buoyancyForce - velY * buoyancyDamping * dampMul;
            rb.AddForceAtPosition(Vector3.up * force, worldPoint, ForceMode.Force);

            avgNormal += normal;
            submergedCount++;
        }

        // 착지 감지: 공중 → 입수 + 빠른 하강 속도
        bool nowSubmerged = submergedCount > 0;
        if (!wasSubmerged && nowSubmerged && rb.velocity.y < -landingVelThreshold)
        {
            float impactSpeed = Mathf.Abs(rb.velocity.y);
            landingDampTimer = landingDampDuration;
            onWaterLanding?.Invoke(impactSpeed);
        }
        else
            landingDampTimer = Mathf.Max(0f, landingDampTimer - Time.fixedDeltaTime);
        wasSubmerged = nowSubmerged;

        // 파도 경사면 슬로프 어시스트: 수면 노멀의 수평 성분을 따라 앞으로 밀어줌
        if (submergedCount > 0 && waveSlopeAssist > 0f)
        {
            avgNormal /= submergedCount;
            // 노멀의 XZ 평면 성분 = 파도 경사 방향(위에서 본 기울기)
            Vector3 slopePush = new Vector3(avgNormal.x, 0f, avgNormal.z) * waveSlopeAssist;
            rb.AddForce(slopePush, ForceMode.Acceleration);
        }
    }

    void ApplyHorizontalDrag()
    {
        // 수평 속도에만 드래그 적용. 수직 속도는 건드리지 않음.
        Vector3 v = rb.velocity;
        Vector3 horizontal = new Vector3(v.x, 0f, v.z);
        rb.AddForce(-horizontal * linearDrag, ForceMode.Acceleration);
    }

    void ApplyMovement()
    {
        float forward = throttleAction?.action.ReadValue<float>() ?? 0f;
        float turn    = steerAction?.action.ReadValue<Vector2>().x ?? 0f;

        // 에디터 테스트용 키보드 fallback
#if UNITY_EDITOR
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (forward == 0f)
                forward = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
            if (turn == 0f)
                turn = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        }
#endif

        if (Mathf.Abs(forward) > 0.01f)
        {
            float boostMul = boost != null ? boost.BoostMultiplier : 1f;
            rb.AddForce(transform.forward * forward * throttle * boostMul, ForceMode.Acceleration);
        }

        if (Mathf.Abs(turn) > 0.01f)
            rb.AddTorque(Vector3.up * turn * turnSpeed * Mathf.Deg2Rad, ForceMode.Acceleration);
    }
}
