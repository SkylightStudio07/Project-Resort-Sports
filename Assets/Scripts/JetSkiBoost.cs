using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class JetSkiBoost : MonoBehaviour
{
    [Header("Events")]
    [Tooltip("부스트 발동 순간 1회 호출. HUD/이펙트/햅틱 트리거용.")]
    public UnityEvent OnBoostActivated;
    [Tooltip("게이지가 막 풀로 찬 순간 1회 호출.")]
    public UnityEvent OnGaugeFull;

    [Header("Gauge")]
    [Tooltip("게이지가 0에서 1까지 차는 데 걸리는 시간(초). 풀스로틀 기준.")]
    public float secondsToFull = 5f;
    [Tooltip("스로틀이 약할 때 충전 효율(0~1).")]
    public AnimationCurve fillByThrottle = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Boost")]
    [Tooltip("부스트 지속 시간(초).")]
    public float boostDuration = 2f;
    [Tooltip("부스트 발동 중 스로틀에 곱할 배수.")]
    public float boostMultiplier = 2f;

    [Header("Twist Gesture (HTC Vive)")]
    [Tooltip("오른손 컨트롤러 Transform. (XR Origin > Camera Offset > Right Controller 같은 노드)")]
    public Transform rightController;
    [Tooltip("이 시간(초) 안에 비틀림 각도가 임계값을 넘으면 발동.")]
    public float twistWindow = 0.4f;
    [Tooltip("발동 임계 각도(도). 손목을 이만큼 빠르게 비틀어야 함.")]
    public float twistThresholdDeg = 60f;

    [Header("Input")]
    [Tooltip("스로틀 입력. JetSkiController 와 같은 액션을 공유해도 됨.")]
    public InputActionReference throttleAction;

    [field: SerializeField] public float Gauge { get; private set; }
    public bool IsBoosting => boostTimeRemaining > 0f;
    public float BoostMultiplier => IsBoosting ? boostMultiplier : 1f;

    private float boostTimeRemaining;

    // twist 추적용 링 버퍼 (간단 구현: 윈도우 시작 시각/각도만 들고 다님)
    private float windowStartTime;
    private float windowStartRoll;
    private bool windowInited;

    void OnEnable()
    {
        throttleAction?.action.Enable();
        ResetWindow();
    }

    void OnDisable()
    {
        throttleAction?.action.Disable();
    }

    void Update()
    {
        float dt = Time.deltaTime;

#if UNITY_EDITOR
        // 에디터 디버그: B 키로 즉시 부스트 발동 (게이지 무관)
        var kb = Keyboard.current;
        if (kb != null && kb.bKey.wasPressedThisFrame && !IsBoosting)
            ActivateBoost();
#endif

        // 1) 부스트 카운트다운
        if (boostTimeRemaining > 0f)
            boostTimeRemaining = Mathf.Max(0f, boostTimeRemaining - dt);

        // 2) 게이지 충전 (부스트 중에는 충전 금지)
        if (!IsBoosting && secondsToFull > 0f)
        {
            float prev = Gauge;
            float throttle = ReadThrottle01();
            float rate = fillByThrottle.Evaluate(throttle) / secondsToFull;
            Gauge = Mathf.Clamp01(Gauge + rate * dt);
            if (prev < 1f && Gauge >= 1f) OnGaugeFull?.Invoke();
        }

        // 3) 트위스트 제스처 (게이지 풀 + 비부스트 상태에서만 검출)
        if (Gauge >= 1f && !IsBoosting && rightController != null)
            DetectTwist();
        else
            ResetWindow();
    }

    float ReadThrottle01()
    {
        float v = 0f;
        if (throttleAction != null && throttleAction.action != null)
            v = Mathf.Clamp01(throttleAction.action.ReadValue<float>());
#if UNITY_EDITOR
        if (v <= 0f)
        {
            var kb = Keyboard.current;
            if (kb != null && kb.wKey.isPressed) v = 1f;
        }
#endif
        return v;
    }

    void DetectTwist()
    {
        float now = Time.time;
        float roll = GetSignedRoll(rightController);

        if (!windowInited || now - windowStartTime > twistWindow)
        {
            windowStartTime = now;
            windowStartRoll = roll;
            windowInited = true;
            return;
        }

        float delta = Mathf.Abs(Mathf.DeltaAngle(windowStartRoll, roll));
        if (delta >= twistThresholdDeg)
            ActivateBoost();
    }

    void ResetWindow()
    {
        windowInited = false;
    }

    // 컨트롤러의 forward 축 기준 roll 각도(±180).
    // Vive 완드를 정면으로 잡고 손목을 비트는 동작 = local Z 축 회전.
    static float GetSignedRoll(Transform t)
    {
        float z = t.localEulerAngles.z;
        return z > 180f ? z - 360f : z;
    }

    void ActivateBoost()
    {
        boostTimeRemaining = boostDuration;
        Gauge = 0f;
        ResetWindow();
        OnBoostActivated?.Invoke();
    }
}
