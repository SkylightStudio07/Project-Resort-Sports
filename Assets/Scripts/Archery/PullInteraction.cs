using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class PullInteraction : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float maxPullDistance = 0.5f;

    private BowController bow;
    private ArrowSpawner arrowSpawner;
    private XRGrabInteractable interactable;
    private Transform pullingHand;
    private bool isPulling = false;
    private float pullAmount = 0f;

    private void Awake()
    {
        bow = GetComponentInParent<BowController>();
        interactable = GetComponent<XRGrabInteractable>();
        arrowSpawner = GetComponentInParent<ArrowSpawner>();

        interactable.selectEntered.AddListener(OnStartPull);
        interactable.selectExited.AddListener(OnStopPull);
    }

    private void OnStartPull(SelectEnterEventArgs args)
    {
        if(!bow.isHeld)
        {
            return;
        }
        pullingHand = args.interactorObject.transform;
        isPulling = true;
        bow.activePull = this;
        arrowSpawner.SpawnArrow();
    }

    private void OnStopPull(SelectExitEventArgs args)
    {
        if (isPulling && pullAmount > 0.1f)
        {
            arrowSpawner.FireArrow(pullAmount);
        }
        else
        {
            arrowSpawner.CancelArrow();
        }
        CancelPull();

    }

    public void CancelPull()
    {
        isPulling = false;
        pullAmount = 0f;
        pullingHand = null;
        bow.activePull = null;
        bow.UpdateString(bow.NockingPoint.position);
    }

    private void Update()
    {
        if  (!isPulling || pullingHand == null)
        {
            return;
        }

        float distance = Vector3.Distance(pullingHand.position, bow.NockingPoint.position);
        pullAmount = Mathf.Clamp01(distance / maxPullDistance);
        bow.UpdateString(pullingHand.position);
    }
}
