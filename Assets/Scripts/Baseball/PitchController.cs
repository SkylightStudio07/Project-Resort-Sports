using UnityEngine;

namespace Baseball
{
    public class PitchController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] GameObject ballPrefab;
        [SerializeField] Transform pitchOrigin;
        [SerializeField] Transform strikeZoneTarget;

        [Header("Pitch Settings")]
        [SerializeField] float fastballSpeed = 15f;
        [SerializeField] float breakingBallSpeed = 11f;

        [Header("Audio")]
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip pitchSound;

        public PitchType CurrentPitchType { get; private set; }

        public void ThrowPitch()
        {
            CurrentPitchType = Random.value > 0.5f ? PitchType.Fastball : PitchType.BreakingBall;

            if (pitchSound != null)
                audioSource.PlayOneShot(pitchSound);

            GameObject ballObj = Instantiate(ballPrefab, pitchOrigin.position, Quaternion.identity);
            BaseballBall ball = ballObj.GetComponent<BaseballBall>();
            ball.Initialize(CurrentPitchType, strikeZoneTarget.position, GetSpeed());
        }

        float GetSpeed() =>
            CurrentPitchType == PitchType.Fastball ? fastballSpeed : breakingBallSpeed;
    }
}
