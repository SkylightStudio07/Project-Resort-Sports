using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BowController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private Transform nockingPoint;
    [SerializeField]
    private Transform wbStringBone;
    [SerializeField]
    private float pullOffset = 0.05f;
    [SerializeField]
    private float maxPullDistance = 0.35f;

    [Header("Camera Attach Settings")]
    [SerializeField]
    private Vector3 attachOffset = new Vector3(0.2f, -0.3f, 0.5f);
    [SerializeField]
    private Vector3 attachRotation = new Vector3(0f, -80f, 0f);

    [HideInInspector]
    public bool isHeld = false;
    [HideInInspector]
    public PullInteraction activePull = null;

    private XRGrabInteractable grabInteractable;
    private ArrowSpawner arrowSpawner;
    private Rigidbody rb;
    private Vector3 defaultStringPos;
    private bool isAttached = false;
    private Transform grabHandTransform;
    private float pullAmount = 0f;

    public Transform NockingPoint => nockingPoint;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();
        arrowSpawner = GetComponent<ArrowSpawner>();

        grabInteractable.trackPosition = false;
        grabInteractable.trackRotation = false;

        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);

        if (wbStringBone != null)
            defaultStringPos = wbStringBone.localPosition;

        UpdateString(0f);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (isAttached) return;

        isHeld = true;
        isAttached = true;
        grabHandTransform = args.interactorObject.transform;

        rb.isKinematic = true;
        transform.SetParent(Camera.main.transform);
        transform.localPosition = attachOffset;
        transform.localRotation = Quaternion.Euler(attachRotation);

        arrowSpawner.SpawnArrow();
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (!isAttached) return;

        if (pullAmount > 0.1f)
            arrowSpawner.FireArrow(pullAmount);
        else
            arrowSpawner.CancelArrow();

        pullAmount = 0f;
        UpdateString(0f);
        isHeld = false;
        isAttached = false;
        grabHandTransform = null;
    }

    private void Update()
    {
        if (!isHeld || grabHandTransform == null) return;

        float distance = Vector3.Distance(grabHandTransform.position, nockingPoint.position);
        pullAmount = Mathf.Clamp01(distance / maxPullDistance);
        UpdateString(pullAmount);
    }

    public void UpdateString(float pull)
    {
        if (wbStringBone == null) return;
        wbStringBone.localPosition = defaultStringPos + new Vector3(0f, 0f, pull * pullOffset);
    }
}
