using UnityEngine;

// 개별 핀의 쓰러짐을 감지
// 핀의 기울기가 임계값(45도) 이상이면 쓰러진 것으로 판정
public class PinBehavior : MonoBehaviour
{
    [Tooltip("쓰러진 것으로 판정할 기울기 각도")]
    public float fallAngleThreshold = 45f;

    [Tooltip("쓰러진 판정을 확정하기까지 유지해야 하는 시간 (초)")]
    public float settleTime = 0.5f;

    public bool IsFallen { get; private set; } = false;

    private float _tiltTimer = 0f;

    private void Update()
    {
        if (IsFallen) return;

        // 핀의 up 벡터와 월드 up의 각도 계산
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
            // 다시 세워지면 타이머 리셋
            _tiltTimer = 0f;
        }
    }

    // 프레임 시작 시 핀 상태를 초기화
    public void ResetPin()
    {
        IsFallen   = false;
        _tiltTimer = 0f;
    }
}
