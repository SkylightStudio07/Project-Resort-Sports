using UnityEngine;

/// <summary>
/// 레인 끝에 배치하는 트리거입니다.
/// 볼이 통과하면 BallManager에 알립니다.
/// Box Collider (Is Trigger ON) 와 함께 사용하세요.
/// </summary>
public class LaneEndTrigger : MonoBehaviour
{
    public BallManager ballManager;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<BowlingBallMovement>() != null)
        {
            Debug.Log("[LaneEndTrigger] 볼이 레인 끝에 도달");
            ballManager.OnBallReachedEnd();
        }
    }
}
