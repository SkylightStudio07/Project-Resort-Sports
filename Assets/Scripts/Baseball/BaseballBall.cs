using System.Collections;
using UnityEngine;

namespace Baseball
{
    public class BaseballBall : MonoBehaviour
    {
        [Header("Breaking Ball Curve")]
        [SerializeField] AnimationCurve curvatureX = AnimationCurve.EaseInOut(0f, 0f, 1f, 1.5f);
        [SerializeField] AnimationCurve curvatureY = AnimationCurve.EaseInOut(0f, 0f, 1f, -0.8f);

        [Header("Strike Zone")]
        [SerializeField] float strikeTriggerDistance = 0.5f;

        PitchType pitchType;
        Vector3 startPos;
        Vector3 targetPos;
        float speed;
        float travelTime;
        float elapsed;

        bool resultReported;
        bool wasHit;

        public bool SwingDetected { get; set; }

        public void Initialize(PitchType type, Vector3 target, float ballSpeed)
        {
            pitchType = type;
            startPos = transform.position;
            targetPos = target;
            speed = ballSpeed;
            travelTime = Vector3.Distance(startPos, targetPos) / speed;
            StartCoroutine(FlyCoroutine());
        }

        IEnumerator FlyCoroutine()
        {
            elapsed = 0f;
            while (elapsed < travelTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / travelTime);

                Vector3 pos = Vector3.Lerp(startPos, targetPos, t);

                if (pitchType == PitchType.BreakingBall)
                {
                    pos.x += curvatureX.Evaluate(t);
                    pos.y += curvatureY.Evaluate(t);
                }

                transform.position = pos;

                // Trigger result check when ball reaches the strike zone
                if (t >= 0.85f && !resultReported && !wasHit)
                {
                    float dist = Vector3.Distance(transform.position, targetPos);
                    if (dist < strikeTriggerDistance)
                    {
                        ReportResult();
                        yield break;
                    }
                }

                yield return null;
            }

            if (!resultReported)
                ReportResult();
        }

        void ReportResult()
        {
            resultReported = true;

            if (!wasHit)
            {
                BallResult result = SwingDetected
                    ? BallResult.PenaltyMiss
                    : (pitchType == PitchType.Fastball ? BallResult.PenaltyNoSwing : BallResult.Success);

                BaseballGameManager.Instance.ReportResult(result);
            }

            Destroy(gameObject, 1f);
        }

        public void OnBatHit(Vector3 batVelocity, Vector3 playerForward)
        {
            if (wasHit || resultReported) return;
            wasHit = true;
            resultReported = true;

            StopAllCoroutines();

            // Forward dot > 0 means ball went forward (success), negative means foul
            float forwardDot = Vector3.Dot(batVelocity.normalized, playerForward);
            BallResult result = forwardDot > 0f ? BallResult.Success : BallResult.Foul;

            BaseballGameManager.Instance.ReportResult(result);

            // Apply physics velocity after hit
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = batVelocity;
            }

            Destroy(gameObject, 3f);
        }
    }
}
