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

    [Header("Camera Attach Settings")]
    [SerializeField]
    private Vector3 attachOffset = new Vector3(0.3f, -0.3f, 0.5f);
    [SerializeField]
    private Vector3 attachRotation = new Vector3(0f, 0f, 0f);

    [HideInInspector]
    public bool isHeld = false;
    [HideInInspector]
    public PullInteraction activePull = null;

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private Vector3 defaultStringPos;
    private bool isAttached = false;

    public Transform NockingPoint => nockingPoint;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);

        if (wbStringBone != null)
        {
            defaultStringPos = wbStringBone.localPosition;
        }
        UpdateString(0f);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (isAttached)
        {
            return;
        }

        isHeld = true;
        isAttached = true;

        rb.isKinematic = true;
        transform.SetParent(Camera.main.transform);
        transform.localPosition = attachOffset;
        transform.localRotation = Quaternion.Euler(attachRotation);

        grabInteractable.enabled = false;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (isAttached)
        {
            return;
        }

        isHeld = false;
        if (activePull != null)
        {
            activePull.CancelPull();
        }
    }

    public void UpdateString(float pullAmount)
    {
        if (wbStringBone == null)
        {
            return;
        }
        wbStringBone.localPosition = defaultStringPos + new Vector3(0f, 0f, pullAmount * pullOffset);
    }
}
