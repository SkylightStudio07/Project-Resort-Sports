using UnityEngine;

public class TargetPatrol : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private float patrolDistance = 3f;
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private bool patrolOnStart = false;

    private Vector3 originPos;
    private bool patrolling = false;
    private float timeOffset = 0f;

    private void Awake()
    {
        originPos = transform.position;
    }

    private void Start()
    {
        if (patrolOnStart)
            StartPatrol();
    }

    public void SetConfig(float distance, float spd)
    {
        patrolDistance = distance;
        speed = spd;
    }

    public void StartPatrol()
    {
        originPos = transform.position;
        timeOffset = Time.time;
        patrolling = true;
    }

    public void StopPatrol()
    {
        patrolling = false;
    }

    private void Update()
    {
        if (!patrolling) return;

        float elapsed = (Time.time - timeOffset) * speed;
        float offset = Mathf.PingPong(elapsed + patrolDistance, patrolDistance * 2f) - patrolDistance;
        transform.position = originPos + transform.right * offset;
    }
}
