using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BowController : MonoBehaviour
{
    [SerializeField] private Transform nockingPoint;
    [SerializeField] private Transform wbStringBone;
    [SerializeField] private float pullOffset = 0.05f;

    [HideInInspector] public bool isHeld = false;
    [HideInInspector] public PullInteraction activePull = null;

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private Vector3 defaultStringPos;

    public Transform NockingPoint => nockingPoint;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);

        if (wbStringBone != null)
            defaultStringPos = wbStringBone.localPosition;

        UpdateString(0f);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isHeld = true;
        rb.isKinematic = true;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isHeld = false;
        rb.isKinematic = false;
        activePull?.CancelPull();
    }

    public void UpdateString(float pull)
    {
        if (wbStringBone == null) return;
        wbStringBone.localPosition = defaultStringPos + new Vector3(0f, 0f, pull * pullOffset);
    }
}
