using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BowController : MonoBehaviour
{
    [Header("레퍼런스")]
    [SerializeField]
    private Transform nockingPoint;
    [SerializeField]
    private Transform stringTopPoint;
    [SerializeField]
    private Transform stringBottomPoint;
    [SerializeField]
    private LineRenderer stringRenderer;

    [HideInInspector]
    public bool isHeld = false;
    [HideInInspector]
    public PullInteraction activePull = null;

    private XRGrabInteractable grabInteractable;

    public Transform NockingPoint => nockingPoint;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);

        UpdateString(nockingPoint.position);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isHeld = true;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isHeld = false;
        if (activePull != null)
            activePull.CancelPull();
    }

    public void UpdateString(Vector3 pullPos)
    {
        if (stringRenderer == null)
        {
            return;
        }
        stringRenderer.SetPosition(0, stringTopPoint.position);
        stringRenderer.SetPosition(1, pullPos);
        stringRenderer.SetPosition(2, stringBottomPoint.position);
    }
}
