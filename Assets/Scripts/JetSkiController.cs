using UnityEngine;
using UnityEngine.InputSystem;
using StylizedWater2;
using UnityEngine.XR;

[RequireComponent(typeof(Rigidbody))]
public class JetSkiController : MonoBehaviour
{
    [Header("Water")]
    public WaterObject waterObject;

    [Header("Buoyancy")]
    public float buoyancyForce = 20f;
    public float buoyancyDamping = 3f;
    public float rollStrength = 1f;

    [Header("Movement")]
    public float throttle = 15f;
    public float turnSpeed = 60f;
    public float linearDrag = 2f;

    [Header("VR Input")]
    [Tooltip("XRI Right Hand/Activate Value — 오른쪽 트리거")]
    public InputActionReference throttleAction;
    [Tooltip("XRI Right Hand/Thumbstick — 오른쪽 트랙패드/스틱 (X축 조향)")]
    public InputActionReference steerAction;

    private Rigidbody rb;

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
        rb.drag = linearDrag;
        rb.angularDrag = 5f;
    }

    void FixedUpdate()
    {
        ApplyBuoyancy();
        ApplyMovement();
    }

    void ApplyBuoyancy()
    {
        if (waterObject == null) return;

        foreach (var offset in sampleOffsets)
        {
            Vector3 worldPoint = transform.TransformPoint(offset);
            float waveHeight = Buoyancy.SampleWaves(worldPoint, waterObject, rollStrength, false, out _);

            float depth = waveHeight - worldPoint.y;
            if (depth <= 0f) continue;

            float velY = rb.GetPointVelocity(worldPoint).y;
            float force = depth * buoyancyForce - velY * buoyancyDamping;
            rb.AddForceAtPosition(Vector3.up * force, worldPoint, ForceMode.Force);
        }
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
            rb.AddForce(transform.forward * forward * throttle, ForceMode.Acceleration);

        if (Mathf.Abs(turn) > 0.01f)
            rb.AddTorque(Vector3.up * turn * turnSpeed * Mathf.Deg2Rad, ForceMode.Acceleration);
    }
}
