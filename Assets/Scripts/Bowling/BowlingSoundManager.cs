using UnityEngine;

/// <summary>
/// 볼링 사운드를 총괄 재생합니다.
/// - 핀 충돌음: 투구당 1회 (PinManager.OnFirstPinHit)
/// - 스트라이크 함성/박수: ScoreManager.OnStrike
/// - 거터 실망 소리: BallManager.OnGutterBall
///
/// [Unity 씬 설정]
/// 1. 빈 오브젝트에 이 스크립트 부착
/// 2. AudioSource 컴포넌트 자동 추가됨
/// 3. 각 AudioClip 슬롯에 사운드 연결
/// 4. PinManager / ScoreManager / BallManager 참조 연결
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BowlingSoundManager : MonoBehaviour
{
    [Header("사운드 클립")]
    public AudioClip pinHitClip;     // 핀 쓰러지는 소리
    public AudioClip strikeClip;     // 스트라이크 박수/함성
    public AudioClip gutterClip;     // 거터 실망 소리

    [Header("볼륨 (0~1)")]
    [Range(0f, 1f)] public float pinHitVolume = 1f;
    [Range(0f, 1f)] public float strikeVolume = 1f;
    [Range(0f, 1f)] public float gutterVolume = 1f;

    [Header("참조")]
    public PinManager   pinManager;
    public ScoreManager scoreManager;
    public BallManager  ballManager;

    private AudioSource _audio;

    // ── Unity 생명주기 ───────────────────────────────────────────────────

    private void Awake()
    {
        _audio = GetComponent<AudioSource>();
        _audio.playOnAwake = false;
    }

    private void Start()
    {
        pinManager.OnFirstPinHit  += HandlePinHit;
        scoreManager.OnStrike     += HandleStrike;
        ballManager.OnGutterBall  += HandleGutter;
    }

    private void OnDestroy()
    {
        if (pinManager   != null) pinManager.OnFirstPinHit -= HandlePinHit;
        if (scoreManager != null) scoreManager.OnStrike    -= HandleStrike;
        if (ballManager  != null) ballManager.OnGutterBall -= HandleGutter;
    }

    // ── 이벤트 핸들러 ────────────────────────────────────────────────────

    private void HandlePinHit() => Play(pinHitClip, pinHitVolume);
    private void HandleStrike() => Play(strikeClip, strikeVolume);
    private void HandleGutter() => Play(gutterClip, gutterVolume);

    // ── 재생 ────────────────────────────────────────────────────────────

    private void Play(AudioClip clip, float volume)
    {
        if (clip != null)
            _audio.PlayOneShot(clip, volume);
    }
}
