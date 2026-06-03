using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("JetSki/Sound Controller")]
public class JetSkiSoundController : MonoBehaviour
{
    [Header("References")]
    public JetSkiController jetSki;
    public JetSkiBoost boost;
    public Rigidbody jetSkiRigidbody;
    public JetSkiCourseStarter courseStarter;

    [Header("Audio Sources")]
    public AudioSource drivingSource;
    public AudioSource boostSource;
    public AudioSource sfxSource;

    [Header("Clips")]
    public AudioClip drivingLoopClip;
    public AudioClip boostClip;
    public AudioClip waterLandingClip;
    public AudioClip gatePerfectClip;
    public AudioClip gatePassClip;

    [Header("Driving")]
    public float minDrivingSpeed = 0.35f;
    public float drivingVolume = 0.7f;
    public float drivingOverlapInterval = 0.45f;
    public float drivingStopGraceSeconds = 0.15f;

    [Header("Boost")]
    public float boostVolume = 0.85f;
    public float boostOverlapInterval = 1.25f;
    public float boostStopGraceSeconds = 0.15f;

    [Header("Landing")]
    public float minLandingImpactSpeed = 3.2f;
    public float landingVolume = 1f;
    public float landingCooldown = 0.25f;

    [Header("Gate")]
    public bool autoSubscribeGates = true;
    public float gateVolume = 1f;

    private readonly List<GateController> subscribedGates = new();
    private float nextLandingTime;
    private float nextDrivingTime;
    private float lastDrivingTime;
    private float nextBoostTime;
    private float lastBoostTime;

    void Reset()
    {
        jetSki = GetComponentInParent<JetSkiController>();
        boost = jetSki != null ? jetSki.GetComponent<JetSkiBoost>() : GetComponentInParent<JetSkiBoost>();
        jetSkiRigidbody = jetSki != null ? jetSki.GetComponent<Rigidbody>() : GetComponentInParent<Rigidbody>();
        courseStarter = FindAnyObjectByType<JetSkiCourseStarter>();
    }

    void Awake()
    {
        ResolveReferences();
        SetupSources();
    }

    void OnEnable()
    {
        ResolveReferences();
        SetupSources();
        Subscribe();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void Update()
    {
        UpdateDrivingLoop();
        UpdateBoostLoop();
    }

    private void ResolveReferences()
    {
        if (jetSki == null)
            jetSki = GetComponentInParent<JetSkiController>();

        if (jetSki == null)
            jetSki = FindAnyObjectByType<JetSkiController>();

        if (boost == null && jetSki != null)
            boost = jetSki.GetComponent<JetSkiBoost>();

        if (boost == null)
            boost = GetComponentInParent<JetSkiBoost>();

        if (boost == null)
            boost = FindAnyObjectByType<JetSkiBoost>();

        if (jetSkiRigidbody == null && jetSki != null)
            jetSkiRigidbody = jetSki.GetComponent<Rigidbody>();

        if (jetSkiRigidbody == null)
            jetSkiRigidbody = GetComponentInParent<Rigidbody>();

        if (courseStarter == null)
            courseStarter = FindAnyObjectByType<JetSkiCourseStarter>();
    }

    private void SetupSources()
    {
        if (drivingSource == null)
        {
            drivingSource = gameObject.AddComponent<AudioSource>();
            drivingSource.playOnAwake = false;
            drivingSource.loop = false;
            drivingSource.spatialBlend = 1f;
        }

        if (boostSource == null)
        {
            boostSource = gameObject.AddComponent<AudioSource>();
            boostSource.playOnAwake = false;
            boostSource.loop = false;
            boostSource.spatialBlend = 1f;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 1f;
        }

        drivingSource.clip = null;
        drivingSource.loop = false;
        drivingSource.playOnAwake = false;
        boostSource.clip = null;
        boostSource.loop = false;
        boostSource.playOnAwake = false;
        sfxSource.playOnAwake = false;
    }

    private void Subscribe()
    {
        if (jetSki != null)
            jetSki.onWaterLanding.AddListener(PlayWaterLanding);

        if (boost != null)
            boost.OnBoostActivated.AddListener(HandleBoostActivated);

        if (autoSubscribeGates)
            SubscribeGates();
    }

    private void Unsubscribe()
    {
        if (jetSki != null)
            jetSki.onWaterLanding.RemoveListener(PlayWaterLanding);

        if (boost != null)
            boost.OnBoostActivated.RemoveListener(HandleBoostActivated);

        foreach (var gate in subscribedGates)
        {
            if (gate != null)
                gate.GatePassedResult -= PlayGatePass;
        }

        subscribedGates.Clear();
    }

    private void SubscribeGates()
    {
        if (courseStarter != null && courseStarter.gates != null && courseStarter.gates.Length > 0)
        {
            foreach (var gate in courseStarter.gates)
                SubscribeGate(gate);

            return;
        }

        foreach (var gate in FindObjectsByType<GateController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            SubscribeGate(gate);
    }

    private void SubscribeGate(GateController gate)
    {
        if (gate == null || subscribedGates.Contains(gate))
            return;

        gate.GatePassedResult += PlayGatePass;
        subscribedGates.Add(gate);
    }

    private void UpdateDrivingLoop()
    {
        if (drivingSource == null || drivingLoopClip == null || jetSkiRigidbody == null)
            return;

        Vector3 velocity = jetSkiRigidbody.velocity;
        float horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        bool isDriving = horizontalSpeed >= minDrivingSpeed;

        if (isDriving)
            lastDrivingTime = Time.time;

        if (!isDriving && Time.time - lastDrivingTime > drivingStopGraceSeconds)
        {
            drivingSource.Stop();
            nextDrivingTime = 0f;
            return;
        }

        if (Time.time < nextDrivingTime)
            return;

        drivingSource.PlayOneShot(drivingLoopClip, drivingVolume);
        nextDrivingTime = Time.time + Mathf.Max(0.05f, drivingOverlapInterval);
    }

    private void HandleBoostActivated()
    {
        nextBoostTime = 0f;
        lastBoostTime = Time.time;
    }

    private void UpdateBoostLoop()
    {
        if (boostSource == null || boostClip == null || boost == null)
            return;

        if (boost.IsBoosting)
            lastBoostTime = Time.time;

        if (!boost.IsBoosting && Time.time - lastBoostTime > boostStopGraceSeconds)
        {
            boostSource.Stop();
            nextBoostTime = 0f;
            return;
        }

        if (Time.time < nextBoostTime)
            return;

        boostSource.PlayOneShot(boostClip, boostVolume);
        nextBoostTime = Time.time + Mathf.Max(0.05f, boostOverlapInterval);
    }

    private void PlayWaterLanding(float impactSpeed)
    {
        if (waterLandingClip == null || sfxSource == null)
            return;

        if (impactSpeed < minLandingImpactSpeed || Time.time < nextLandingTime)
            return;

        nextLandingTime = Time.time + landingCooldown;
        float impactVolume = landingVolume * Mathf.Clamp(impactSpeed / minLandingImpactSpeed, 0.75f, 1.25f);
        sfxSource.PlayOneShot(waterLandingClip, impactVolume);
    }

    private void PlayGatePass(bool isPerfect)
    {
        if (sfxSource == null)
            return;

        AudioClip clip = isPerfect ? gatePerfectClip : gatePassClip;
        if (clip != null)
            sfxSource.PlayOneShot(clip, gateVolume);
    }
}
