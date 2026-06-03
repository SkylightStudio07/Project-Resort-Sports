using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[AddComponentMenu("JetSki/Session Manager")]
public class JetSkiSessionManager : MonoBehaviour
{
    public static JetSkiSessionManager Instance { get; private set; }

    [Header("Scene References")]
    public JetSkiScoreManager scoreManager;
    public JetSkiCourseStarter courseStarter;
    public JetSkiTutorialOverlay tutorialOverlay;
    public Transform gameStartPosition;
    public Transform respawnTarget;

    [Header("Session")]
    public bool resetVelocityOnRestart = true;
    public bool keepUprightOnRestart = true;
    public bool showTutorialOnRestart = true;
    public float tutorialLockSeconds = 3f;

    [Header("Mode Objects")]
    public bool applyModeObjectState = false;
    public GameObject[] hubModeObjects;
    public GameObject[] jetSkiModeObjects;

    [Header("Hub")]
    public string hubWorldSceneName = "HubWorld";
    public bool loadHubSceneOnExit = false;

    [Header("Events")]
    public UnityEvent onSessionPrepared;
    public UnityEvent onSessionRestarted;
    public UnityEvent onHubReturnRequested;

    private Coroutine tutorialCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveReferences();
    }

    void Start()
    {
        ResolveReferences();
    }

    public void PrepareSession(bool showTutorial = true)
    {
        ResolveReferences();
        SetModeObjects(jetSkiActive: true);

        if (scoreManager != null)
            scoreManager.ResetForRetry(false);

        MoveToStartPosition();

        if (courseStarter != null)
            courseStarter.PrepareRetry(respawnTarget);

        if (showTutorial && showTutorialOnRestart)
            ShowTutorialWithRetryDelay();

        onSessionPrepared?.Invoke();
    }

    public void RestartSession()
    {
        PrepareSession(true);
        onSessionRestarted?.Invoke();
    }

    public void ReturnToHubWorld()
    {
        onHubReturnRequested?.Invoke();
        SetModeObjects(jetSkiActive: false);

        if (!loadHubSceneOnExit)
            return;

        if (string.IsNullOrWhiteSpace(hubWorldSceneName))
        {
            Debug.LogWarning("[JetSkiSessionManager] Hub world scene name is empty.");
            return;
        }

        SceneManager.LoadScene(hubWorldSceneName);
    }

    public void EnterJetSkiMode()
    {
        PrepareSession(true);
    }

    public void EnterHubMode()
    {
        ReturnToHubWorld();
    }

    public void ResolveReferences()
    {
        if (scoreManager == null)
            scoreManager = JetSkiScoreManager.Instance;

        if (courseStarter == null)
            courseStarter = FindAnyObjectByType<JetSkiCourseStarter>();

        if (respawnTarget == null)
            respawnTarget = GetComponentInParent<JetSkiController>()?.transform;

        if (respawnTarget == null)
            respawnTarget = FindAnyObjectByType<JetSkiController>()?.transform;

        if (tutorialOverlay == null)
            tutorialOverlay = GetComponentInParent<JetSkiTutorialOverlay>(true);

        if (tutorialOverlay == null && respawnTarget != null)
            tutorialOverlay = respawnTarget.GetComponentInChildren<JetSkiTutorialOverlay>(true);

        if (tutorialOverlay == null)
            tutorialOverlay = FindTutorialOverlayInScene();

        if (scoreManager != null && tutorialOverlay != null)
            scoreManager.tutorialOverlay = tutorialOverlay;
    }

    private void ShowTutorialWithRetryDelay()
    {
        TryShowTutorial(true);

        if (tutorialCoroutine != null)
            StopCoroutine(tutorialCoroutine);

        tutorialCoroutine = StartCoroutine(ShowTutorialAfterPhysicsStep());
    }

    private IEnumerator ShowTutorialAfterPhysicsStep()
    {
        yield return null;
        yield return new WaitForFixedUpdate();

        ResolveReferences();

        if (scoreManager != null)
            scoreManager.ResetForRetry(false);

        if (courseStarter != null)
            courseStarter.PrepareRetry(respawnTarget);

        if (TryShowTutorial(false))
            Debug.Log("[JetSkiSessionManager] Tutorial shown for session restart.");
        else
            Debug.LogWarning("[JetSkiSessionManager] Tutorial requested, but no JetSkiTutorialOverlay was found.");

        tutorialCoroutine = null;
    }

    private bool TryShowTutorial(bool logMissing)
    {
        ResolveReferences();

        if (tutorialOverlay == null)
        {
            if (logMissing)
                Debug.LogWarning("[JetSkiSessionManager] Tutorial requested, but no JetSkiTutorialOverlay was found.");

            return false;
        }

        tutorialOverlay.ShowForRetry(tutorialLockSeconds);
        return true;
    }

    private void MoveToStartPosition()
    {
        if (gameStartPosition == null || respawnTarget == null)
        {
            Debug.LogWarning("[JetSkiSessionManager] Session needs both Game Start Position and Respawn Target.");
            return;
        }

        Vector3 respawnPosition = gameStartPosition.position;
        Quaternion respawnRotation = GetRespawnRotation();
        var rb = respawnTarget.GetComponent<Rigidbody>();

        if (rb != null)
        {
            if (resetVelocityOnRestart)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.position = respawnPosition;
            rb.rotation = respawnRotation;
            respawnTarget.SetPositionAndRotation(respawnPosition, respawnRotation);
            Physics.SyncTransforms();

            if (resetVelocityOnRestart)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            rb.Sleep();
            return;
        }

        respawnTarget.SetPositionAndRotation(respawnPosition, respawnRotation);
        Physics.SyncTransforms();
    }

    private Quaternion GetRespawnRotation()
    {
        if (!keepUprightOnRestart)
            return gameStartPosition.rotation;

        Vector3 forward = Vector3.ProjectOnPlane(gameStartPosition.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f && respawnTarget != null)
            forward = Vector3.ProjectOnPlane(respawnTarget.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    private JetSkiTutorialOverlay FindTutorialOverlayInScene()
    {
        foreach (var overlay in Resources.FindObjectsOfTypeAll<JetSkiTutorialOverlay>())
        {
            if (overlay == null) continue;
            if (!overlay.gameObject.scene.IsValid()) continue;
            return overlay;
        }

        return null;
    }

    private void SetModeObjects(bool jetSkiActive)
    {
        if (!applyModeObjectState)
            return;

        SetActive(hubModeObjects, !jetSkiActive);
        SetActive(jetSkiModeObjects, jetSkiActive);
    }

    private void SetActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        foreach (var obj in objects)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }
}
