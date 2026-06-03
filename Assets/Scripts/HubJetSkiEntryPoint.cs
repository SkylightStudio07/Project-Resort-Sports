using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
[AddComponentMenu("JetSki/Hub Entry Point")]
public class HubJetSkiEntryPoint : MonoBehaviour
{
    [Header("Detection")]
    public string playerTag = "Player";
    public bool acceptAnyCollider = false;
    public GameObject promptRoot;

    [Header("Launch")]
    public JetSkiSessionManager sessionManager;
    public bool startSessionInCurrentScene = true;
    public string jetSkiSceneName = "Jetski Draft GameScene Day";
    public bool loadSceneOnStart = false;

    [Header("Events")]
    public UnityEvent onPromptShown;
    public UnityEvent onPromptHidden;
    public UnityEvent onStartRequested;

    private bool playerInside;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Awake()
    {
        HidePrompt();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerInside = true;
        ShowPrompt();
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        playerInside = false;
        HidePrompt();
    }

    public void StartJetSki()
    {
        if (!playerInside && !acceptAnyCollider)
            return;

        HidePrompt();
        onStartRequested?.Invoke();

        if (startSessionInCurrentScene)
        {
            ResolveSessionManager();

            if (sessionManager != null)
            {
                sessionManager.PrepareSession(true);
                return;
            }

            Debug.LogWarning("[HubJetSkiEntryPoint] Start requested, but no JetSkiSessionManager was found.");
            return;
        }

        if (!loadSceneOnStart)
            return;

        if (string.IsNullOrWhiteSpace(jetSkiSceneName))
        {
            Debug.LogWarning("[HubJetSkiEntryPoint] Jet ski scene name is empty.");
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(jetSkiSceneName);
    }

    public void ShowPrompt()
    {
        if (promptRoot != null)
            promptRoot.SetActive(true);

        onPromptShown?.Invoke();
    }

    public void HidePrompt()
    {
        if (promptRoot != null)
            promptRoot.SetActive(false);

        onPromptHidden?.Invoke();
    }

    private bool IsPlayer(Collider other)
    {
        if (acceptAnyCollider)
            return true;

        if (other.CompareTag(playerTag))
            return true;

        var root = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.transform.root.gameObject;
        return root.CompareTag(playerTag);
    }

    private void ResolveSessionManager()
    {
        if (sessionManager == null)
            sessionManager = FindAnyObjectByType<JetSkiSessionManager>();
    }
}
