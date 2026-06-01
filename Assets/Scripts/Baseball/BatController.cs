using UnityEngine;

namespace Baseball
{
    public class BatController : MonoBehaviour
    {
        [Header("Controller Transforms")]
        [SerializeField] Transform leftController;
        [SerializeField] Transform rightController;

        [Header("Bat Visual")]
        [SerializeField] Transform batVisual;

        [Header("Swing Settings")]
        [SerializeField] float swingVelocityThreshold = 3f;
        [SerializeField] float swingCooldown = 0.5f;

        [Header("Audio")]
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip swingSound;
        [SerializeField] AudioClip hitSound;

        Transform playerHead;

        Vector3 prevMidpoint;
        float lastSwingTime = -999f;

        BaseballBall currentBall;

        void Start()
        {
            prevMidpoint = GetMidpoint();
            // Use main camera as player head reference for forward direction
            playerHead = Camera.main?.transform;
        }

        void Update()
        {
            Vector3 midpoint = GetMidpoint();
            Vector3 velocity = (midpoint - prevMidpoint) / Time.deltaTime;

            UpdateBatVisual(midpoint);

            if (velocity.magnitude > swingVelocityThreshold && CanSwing())
            {
                OnSwing(velocity);
            }

            prevMidpoint = midpoint;
        }

        Vector3 GetMidpoint() =>
            (leftController.position + rightController.position) * 0.5f;

        void UpdateBatVisual(Vector3 midpoint)
        {
            if (batVisual == null) return;
            batVisual.position = midpoint;
            Vector3 dir = rightController.position - leftController.position;
            if (dir.sqrMagnitude > 0.001f)
                batVisual.rotation = Quaternion.LookRotation(dir);
        }

        bool CanSwing() => Time.time - lastSwingTime > swingCooldown;

        void OnSwing(Vector3 velocity)
        {
            lastSwingTime = Time.time;

            if (swingSound != null)
                audioSource.PlayOneShot(swingSound);

            if (currentBall != null)
                currentBall.SwingDetected = true;
        }

        void OnTriggerEnter(Collider other)
        {
            BaseballBall ball = other.GetComponent<BaseballBall>();
            if (ball == null) return;

            currentBall = ball;
        }

        void OnCollisionEnter(Collision collision)
        {
            BaseballBall ball = collision.gameObject.GetComponent<BaseballBall>();
            if (ball == null) return;

            if (hitSound != null)
                audioSource.PlayOneShot(hitSound);

            Vector3 swingVel = (GetMidpoint() - prevMidpoint) / Time.deltaTime;
            Vector3 forward = playerHead != null ? playerHead.forward : Vector3.forward;
            forward.y = 0f;
            forward.Normalize();

            ball.OnBatHit(swingVel, forward);
            currentBall = null;
        }
    }
}
