using System.Collections;
using UnityEngine;
using UnityEngine.XR;

// 부스트 발동 시 파티클 + 햅틱 + 카메라 FOV 펌프.
// JetSkiBoost.OnBoostActivated 에 인스펙터에서 연결하거나, 자동 연결.
public class JetSkiBoostFx : MonoBehaviour
{
    [Header("Source")]
    public JetSkiBoost boost;

    [Header("Particles")]
    [Tooltip("부스트 발동 시 Play() 호출. 엔진 분사 같은 ParticleSystem.")]
    public ParticleSystem boostParticle;

    [Header("Haptic")]
    [Tooltip("어느 손에 햅틱 보낼지.")]
    public XRNode hapticNode = XRNode.RightHand;
    [Range(0f, 1f)] public float hapticAmplitude = 0.8f;
    public float hapticDuration = 0.4f;

    [Header("Camera FOV Pump")]
    [Tooltip("부스트 동안 살짝 줌인되는 카메라. 비워두면 Camera.main.")]
    public Camera targetCamera;
    [Tooltip("부스트 동안 더해지는 FOV. 부스트 끝나면 원래 값으로 복원.")]
    public float fovBump = 10f;
    [Tooltip("FOV 변화 가속/감속 시간(초).")]
    public float fovEase = 0.15f;

    private float baseFov;
    private Coroutine fovRoutine;

    void Reset()
    {
        boost = GetComponent<JetSkiBoost>();
    }

    void Awake()
    {
        if (boost == null) boost = GetComponent<JetSkiBoost>();
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera != null) baseFov = targetCamera.fieldOfView;
    }

    void OnEnable()
    {
        if (boost != null)
        {
            boost.OnBoostActivated.AddListener(HandleBoostActivated);
        }
    }

    void OnDisable()
    {
        if (boost != null)
        {
            boost.OnBoostActivated.RemoveListener(HandleBoostActivated);
        }
    }

    void HandleBoostActivated()
    {
        if (boostParticle != null) boostParticle.Play();
        SendHaptic();
        if (targetCamera != null)
        {
            if (fovRoutine != null) StopCoroutine(fovRoutine);
            fovRoutine = StartCoroutine(FovPump(boost.boostDuration));
        }
    }

    void SendHaptic()
    {
        var device = InputDevices.GetDeviceAtXRNode(hapticNode);
        if (device.isValid && device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse)
            device.SendHapticImpulse(0u, hapticAmplitude, hapticDuration);
    }

    IEnumerator FovPump(float duration)
    {
        // 0..fovEase: ramp up. duration-fovEase..duration: ramp down.
        float t = 0f;
        while (t < fovEase)
        {
            t += Time.deltaTime;
            targetCamera.fieldOfView = Mathf.Lerp(baseFov, baseFov + fovBump, t / fovEase);
            yield return null;
        }
        targetCamera.fieldOfView = baseFov + fovBump;

        float hold = Mathf.Max(0f, duration - fovEase * 2f);
        yield return new WaitForSeconds(hold);

        t = 0f;
        while (t < fovEase)
        {
            t += Time.deltaTime;
            targetCamera.fieldOfView = Mathf.Lerp(baseFov + fovBump, baseFov, t / fovEase);
            yield return null;
        }
        targetCamera.fieldOfView = baseFov;
        fovRoutine = null;
    }
}
