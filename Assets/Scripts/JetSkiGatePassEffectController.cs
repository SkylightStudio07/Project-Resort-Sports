using System.Collections.Generic;
using System.Collections;
using UnityEngine;

[AddComponentMenu("JetSki/Gate Pass Effect Controller")]
public class JetSkiGatePassEffectController : MonoBehaviour
{
    [Header("References")]
    public JetSkiCourseStarter courseStarter;
    public JetSkiTutorialOverlay tutorialOverlay;
    public Transform effectAnchor;

    [Header("Effect Prefabs")]
    public GameObject gatePassEffectPrefab;
    public GameObject gatePerfectEffectPrefab;

    [Header("Placement")]
    public bool spawnInFrontOfCamera = true;
    public float cameraDistance = 1.5f;
    public bool parentToCamera = true;
    public bool forceLocalParticleSimulation = true;
    public Vector3 localEffectOffset = Vector3.zero;
    public Vector3 worldEffectOffset = Vector3.zero;
    public bool faceMainCamera = true;

    [Header("Lifetime")]
    public bool autoSubscribeGates = true;
    public float destroyAfterSeconds = 3f;
    public float minSecondsBetweenSpawns = 0.15f;
    public bool replaceActiveEffect = true;

    private readonly List<GateController> subscribedGates = new();
    private Coroutine pendingSpawnRoutine;
    private GameObject activeEffect;
    private bool pendingIsPerfect;
    private float nextSpawnAllowedTime;

    void Reset()
    {
        courseStarter = FindAnyObjectByType<JetSkiCourseStarter>();
        tutorialOverlay = GetComponentInParent<JetSkiTutorialOverlay>();
    }

    void OnEnable()
    {
        ResolveReferences();
        Subscribe();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    private void ResolveReferences()
    {
        if (courseStarter == null)
            courseStarter = FindAnyObjectByType<JetSkiCourseStarter>();

        if (tutorialOverlay == null)
            tutorialOverlay = GetComponentInParent<JetSkiTutorialOverlay>(true);

        if (tutorialOverlay == null)
            tutorialOverlay = FindAnyObjectByType<JetSkiTutorialOverlay>();
    }

    private void Subscribe()
    {
        if (!autoSubscribeGates)
            return;

        if (courseStarter != null && courseStarter.gates != null && courseStarter.gates.Length > 0)
        {
            foreach (var gate in courseStarter.gates)
                SubscribeGate(gate);

            return;
        }

        foreach (var gate in FindObjectsByType<GateController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            SubscribeGate(gate);
    }

    private void Unsubscribe()
    {
        foreach (var gate in subscribedGates)
        {
            if (gate != null)
                gate.GatePassedResult -= QueueGateEffect;
        }

        subscribedGates.Clear();
    }

    private void SubscribeGate(GateController gate)
    {
        if (gate == null || subscribedGates.Contains(gate))
            return;

        gate.GatePassedResult += QueueGateEffect;
        subscribedGates.Add(gate);
    }

    private void QueueGateEffect(bool isPerfect)
    {
        if (Time.time < nextSpawnAllowedTime)
            return;

        pendingIsPerfect |= isPerfect;

        if (pendingSpawnRoutine == null)
            pendingSpawnRoutine = StartCoroutine(SpawnQueuedGateEffect());
    }

    private IEnumerator SpawnQueuedGateEffect()
    {
        yield return null;

        SpawnGateEffect(pendingIsPerfect);
        pendingIsPerfect = false;
        pendingSpawnRoutine = null;
        nextSpawnAllowedTime = Time.time + Mathf.Max(0f, minSecondsBetweenSpawns);
    }

    private void SpawnGateEffect(bool isPerfect)
    {
        GameObject prefab = isPerfect ? gatePerfectEffectPrefab : gatePassEffectPrefab;
        if (prefab == null)
            return;

        if (replaceActiveEffect && activeEffect != null)
            Destroy(activeEffect);

        GetSpawnPose(out Vector3 position, out Quaternion rotation);
        var instance = Instantiate(prefab, position, rotation);
        activeEffect = instance;
        ParentToCameraIfNeeded(instance);
        ConfigureParticleSystems(instance);
        PlayParticleSystems(instance);

        if (destroyAfterSeconds > 0f)
            Destroy(instance, destroyAfterSeconds);
    }

    private void GetSpawnPose(out Vector3 position, out Quaternion rotation)
    {
        var targetCamera = GetTargetCamera();

        if (spawnInFrontOfCamera && targetCamera != null)
        {
            Transform cameraTransform = targetCamera.transform;
            position = cameraTransform.position
                + cameraTransform.forward * cameraDistance
                + cameraTransform.TransformVector(localEffectOffset)
                + worldEffectOffset;
            rotation = Quaternion.LookRotation(-cameraTransform.forward, cameraTransform.up);
            return;
        }

        if (effectAnchor != null)
        {
            position = effectAnchor.TransformPoint(localEffectOffset) + worldEffectOffset;
            rotation = effectAnchor.rotation;
        }
        else if (tutorialOverlay != null)
        {
            position = tutorialOverlay.transform.TransformPoint(tutorialOverlay.localPosition + localEffectOffset) + worldEffectOffset;
            rotation = tutorialOverlay.transform.rotation * Quaternion.Euler(tutorialOverlay.localEulerAngles);
        }
        else
        {
            position = transform.TransformPoint(localEffectOffset) + worldEffectOffset;
            rotation = transform.rotation;
        }

        if (!faceMainCamera)
            return;

        if (targetCamera == null)
            return;

        Vector3 toCamera = targetCamera.transform.position - position;
        if (toCamera.sqrMagnitude > 0.0001f)
            rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
    }

    private void ParentToCameraIfNeeded(GameObject instance)
    {
        if (!spawnInFrontOfCamera || !parentToCamera || instance == null)
            return;

        var targetCamera = GetTargetCamera();
        if (targetCamera != null)
            instance.transform.SetParent(targetCamera.transform, true);
    }

    private Camera GetTargetCamera()
    {
        var targetCamera = Camera.main;
        if (targetCamera == null)
            targetCamera = FindAnyObjectByType<Camera>();

        return targetCamera;
    }

    private void ConfigureParticleSystems(GameObject instance)
    {
        if (!forceLocalParticleSimulation || instance == null)
            return;

        foreach (var particleSystem in instance.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = particleSystem.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
        }
    }

    private void PlayParticleSystems(GameObject instance)
    {
        foreach (var particleSystem in instance.GetComponentsInChildren<ParticleSystem>(true))
            particleSystem.Play();
    }
}
