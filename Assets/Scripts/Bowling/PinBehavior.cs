using UnityEngine;

/// <summary>
/// 개별 핀의 쓰러짐을 감지합니다.
/// 핀의 기울기가 임계값(45도) 이상이면 쓰러진 것으로 판정합니다.
/// ResetPin() 호출 시 초기 위치/회전/물리 상태를 복원합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PinBehavior : MonoBehaviour
{
    [Tooltip("쓰러진 것으로 판정할 기울기 각도")]
    public float fallAngleThreshold = 45f;

    [Tooltip("쓰러진 판정을 확정하기까지 유지해야 하는 시간 (초)")]
    public float settleTime = 0.5f;

    public bool IsFallen { get; private set; } = false;

    private float    _tiltTimer = 0f;
    private Rigidbody _rb;

    // 초기 위치/회전 저장
    private Vector3    _initialPosition;
    private Quaternion _initialRotation;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // 씬 배치 위치/회전을 초기값으로 저장
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
    }

    private void Update()
    {
        if (IsFallen) return;

        float angle = Vector3.Angle(Vector3.up, transform.up);

        if (angle > fallAngleThreshold)
        {
            _tiltTimer += Time.deltaTime;
            if (_tiltTimer >= settleTime)
            {
                IsFallen = true;
                Debug.Log($"[Pin] {gameObject.name} 쓰러짐");
            }
        }
        else
        {
            _tiltTimer = 0f;
        }
    }

    /// <summary>
    /// 핀을 초기 상태로 완전히 복원합니다.
    /// 위치, 회전, Rigidbody 속도까지 모두 초기화합니다.
    /// </summary>
    public void ResetPin()
    {
        // 물리 속도 초기화
        _rb.velocity  = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        // 위치/회전 복원
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;

        // 상태 초기화
        IsFallen   = false;
        _tiltTimer = 0f;
    }
}
