using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BowController : MonoBehaviour
{
    [SerializeField] private Transform nockingPoint;
    [SerializeField] private Transform wbStringBone;
    [SerializeField] private float pullOffset = 0.05f;

    [Header("Camera Attach Settings")]
    [SerializeField] private Vector3 attachOffset = new Vector3(0.2f, -0.3f, 0.5f);
    [SerializeField] private Vector3 attachRotation = new Vector3(0f, -80f, 0f);

    [HideInInspector] public bool isHeld = false;
    [HideInInspector] public PullInteraction activePull = null;

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private Vector3 defaultStringPos;
    private Camera mainCamera;

    public Transform NockingPoint => nockingPoint;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();
        mainCamera = Camera.main;

        grabInteractable.trackPosition = false;
        grabInteractable.trackRotation = false;
        grabInteractable.selectEntered.AddListener(OnGrabbed);

        if (wbStringBone != null)
            defaultStringPos = wbStringBone.localPosition;

        UpdateString(0f);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (isHeld) return;

        isHeld = true;
        rb.isKinematic = true;
        transform.SetParent(mainCamera.transform);
        transform.localPosition = attachOffset;
        transform.localRotation = Quaternion.Euler(attachRotation);
        grabInteractable.enabled = false;
    }

    public void UpdateString(float pull)
    {
        if (wbStringBone == null) return;
        wbStringBone.localPosition = defaultStringPos + new Vector3(0f, 0f, pull * pullOffset);
    }
}
