using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// 볼링공 잡기 + 굴리기
// - Grip + Trigger + Thumbstick(누름) 세 버튼 동시에 눌러야 잡힘
// - 놓으면 손 스윙 속도로 레인에 굴러감
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class BowlingBallMovement : MonoBehaviour
{
    [Header("Input Actions")]
    [Tooltip("오른손 Trigger 버튼 (RightHand/Activate)")]
    public InputActionReference rightTriggerAction;

    [Tooltip("오른손 Thumbstick 누름 (RightHand/Primary2DAxisClick)")]
    public InputActionReference rightThumbAction;

    [Header("투구 설정")]
    [Tooltip("릴리즈 속도 배율")]
    public float throwMultiplier = 3.0f;

    [Tooltip("최소 투구 속도 (m/s)")]
    public float minThrowSpeed = 5f;

    [Tooltip("최대 투구 속도 (m/s)")]
    public float maxThrowSpeed = 15f;

    [Tooltip("레인 방향 (볼이 굴러갈 forward 기준 오브젝트)")]
    public Transform laneForwardReference;

    public BallManager ballManager;

    /// <summary>거터에 진입했을 때 발생합니다. NotificationUI가 구독합니다.</summary>
    public event Action OnGutterEntered;

    private XRGrabInteractable grab;
    private Rigidbody          rb;

    // 손 위치 샘플링 (릴리즈 순간 속도 계산용)
    private Vector3 _prevHandPos;
    private Vector3 _handVelocity;

    // 제대로 잡혔을 때만 true → ThrowBall() 실행 조건
    private bool _isProperlyGrabbed = false;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb   = GetComponent<Rigidbody>();

        // XRGrabInteractable 이벤트 연결
        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);

        // 직접 속도 계산
        grab.throwOnDetach = false;
    }

    private void OnEnable()
    {
        rightTriggerAction?.action.Enable();
        rightThumbAction?.action.Enable();
    }

    private void OnDisable()
    {
        rightTriggerAction?.action.Disable();
        rightThumbAction?.action.Disable();
    }

    private void Update()
    {
        // 잡고 있는 동안 매 프레임 손 속도 샘플링
        if (grab.isSelected)
        {
            Vector3 handPos   = GetHandPosition();
            _handVelocity     = (handPos - _prevHandPos) / Time.deltaTime;
            _prevHandPos      = handPos;

            // 세 버튼 중 하나라도 떼면 강제 릴리즈
            if (!AllButtonsHeld())
            {
                grab.interactionManager.SelectExit(grab.interactorsSelecting[0], grab);
            }
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // 잡는 순간 세 버튼이 모두 눌려 있지 않으면 즉시 놓기
        if (!AllButtonsHeld())
        {
            _isProperlyGrabbed = false; // 제대로 안 잡힌 상태로 명시
            grab.interactionManager.SelectExit(args.interactorObject, grab);
            return;
        }

        _isProperlyGrabbed = true;
        _prevHandPos       = GetHandPosition();
        _handVelocity      = Vector3.zero;

        Debug.Log("[BowlingBall] 잡았다!");
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        // 제대로 잡힌 상태에서 놓았을 때만 투구
        if (!_isProperlyGrabbed) return;

        _isProperlyGrabbed = false;
        ThrowBall();
    }

    private void ThrowBall()
    {
        Vector3 velocity = _handVelocity * throwMultiplier;

        if (laneForwardReference != null)
        {
            Vector3 forward = laneForwardReference.forward;
            Vector3 right   = laneForwardReference.right;

            // 전진 속도 (레인 방향)
            float forwardSpeed = Vector3.Dot(velocity, forward);
            forwardSpeed       = Mathf.Clamp(forwardSpeed, minThrowSpeed, maxThrowSpeed);

            // 좌우 속도 (손 스윙 좌우 성분 반영, 전진 속도의 50%로 제한)
            float sideSpeed = Vector3.Dot(velocity, right);
            sideSpeed       = Mathf.Clamp(sideSpeed, -forwardSpeed * 0.5f, forwardSpeed * 0.5f);

            velocity = forward * forwardSpeed + right * sideSpeed;
        }

        rb.isKinematic     = false;
        rb.velocity        = velocity;
        rb.angularVelocity = new Vector3(velocity.magnitude * 0.5f, 0f, 0f);

        ballManager?.OnBallThrown();

        Debug.Log($"[BowlingBall] 투구! 속도: {velocity.magnitude:F1} m/s  좌우: {velocity.x:F1}");
    }


    // Grip + Trigger 두 버튼이 모두 눌려있는지 확인
    private bool AllButtonsHeld()
    {
        bool grip    = grab.isSelected;
        bool trigger = rightTriggerAction != null &&
                       rightTriggerAction.action.ReadValue<float>() > 0.5f;

        // rightThumbAction이 연결돼 있으면 추가 조건으로 체크, 없으면 무시
        bool thumb = rightThumbAction == null ||
                     rightThumbAction.action.ReadValue<float>() > 0.5f;

        return grip && trigger && thumb;
    }

    private Vector3 GetHandPosition()
    {
        if (grab.isSelected && grab.interactorsSelecting.Count > 0)
            return grab.interactorsSelecting[0].transform.position;
        return transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Gutter"))
        {
            // 속도 크기는 유지하고 방향을 레인 직진 방향으로 강제 전환
            float   speed        = rb.velocity.magnitude;
            Vector3 gutterDir    = laneForwardReference != null
                                   ? laneForwardReference.forward
                                   : transform.forward;
            rb.velocity          = gutterDir * speed;
            rb.angularVelocity   = new Vector3(speed * 0.5f, 0f, 0f);
            OnGutterEntered?.Invoke();
            Debug.Log("[BowlingBall] 거터!");
        }

        if (other.CompareTag("LaneEnd"))
        {
            ballManager?.OnBallReachedEnd();
        }
    }

    /// <summary>BallManager에서 잡기 활성/비활성 제어 시 호출합니다.</summary>
    public void SetGrabbable(bool enabled)
    {
        grab.enabled = enabled;
    }

    /// <summary>BallManager에서 볼 복귀 시 호출합니다.</summary>
    public void ReturnToSpawn(Transform spawnPoint)
    {
        rb.velocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic     = true;

        transform.position = spawnPoint.position;
        transform.rotation = spawnPoint.rotation;

        rb.isKinematic = false;
    }
}
