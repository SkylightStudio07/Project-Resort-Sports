using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BowController : MonoBehaviour
{
    [SerializeField] private Transform nockingPoint;
    [SerializeField] private Transform wbStringBone;
    [SerializeField] private float pullOffset = 0.05f;
    [SerializeField] private float maxPullDistance = 0.35f;

    [Header("Left Hand Settings")]
    [SerializeField] private Vector3 leftHandPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 leftHandRotationOffset = Vector3.zero;

    [Header("Aim Crosshair")]
    [SerializeField] private Transform aimCrosshair;
    [SerializeField] private LineRenderer aimLine;
    [SerializeField] private float aimMaxDistance = 50f;

    [Header("Haptic")]
    [SerializeField] private float maxHapticAmplitude = 0.6f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip drawClip;
    [SerializeField] private AudioClip fireClip;

    [HideInInspector] public bool isHeld = false;
    [HideInInspector] public PullInteraction activePull = null;

    private XRGrabInteractable grabInteractable;
    private ArrowSpawner arrowSpawner;
    private Rigidbody rb;
    private Vector3 defaultStringPos;
    private Camera mainCamera;
    private bool isDrawing = false;
    private bool prevTrigger = false;
    private float pullAmount = 0f;
    private float drawStartDistance = 0f;

    public Transform NockingPoint => nockingPoint;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();
        arrowSpawner = GetComponent<ArrowSpawner>();
        mainCamera = Camera.main;

        grabInteractable.trackPosition = false;
        grabInteractable.trackRotation = false;
        grabInteractable.selectEntered.AddListener(OnGrabbed);

        if (wbStringBone != null)
            defaultStringPos = wbStringBone.localPosition;

        UpdateString(0f);

        if (aimCrosshair != null) aimCrosshair.gameObject.SetActive(false);
        if (aimLine != null) aimLine.enabled = false;
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (isHeld) return;

        isHeld = true;
        rb.isKinematic = true;
        grabInteractable.enabled = false;
    }

    private void Update()
    {
        if (!isHeld) return;

        TrackLeftHand();

        var rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        rightHand.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue);
        bool triggerDown = triggerValue > 0.5f;

        if (triggerDown && !prevTrigger)
            OnTriggerPressed();
        else if (!triggerDown && prevTrigger)
            OnTriggerReleased();

        prevTrigger = triggerDown;

        if (isDrawing)
        {
            rightHand.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 localPos);
            Vector3 rightHandWorldPos = mainCamera.transform.parent != null
                ? mainCamera.transform.parent.TransformPoint(localPos)
                : localPos;

            float distance = Vector3.Distance(rightHandWorldPos, nockingPoint.position);
            float pullDelta = distance - drawStartDistance;
            pullAmount = Mathf.Clamp01(pullDelta / maxPullDistance);
            UpdateString(pullAmount);
            rightHand.SendHapticImpulse(0, pullAmount * maxHapticAmplitude, Time.deltaTime);
        }

        UpdateAimCrosshair();
    }

    private void TrackLeftHand()
    {
        var leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (!leftHand.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 localPos)) return;
        if (!leftHand.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion localRot)) return;

        Transform origin = mainCamera.transform.parent;
        if (origin != null)
        {
            transform.position = origin.TransformPoint(localPos);
            transform.rotation = origin.rotation * localRot * Quaternion.Euler(leftHandRotationOffset);
        }
        else
        {
            transform.position = localPos;
            transform.rotation = localRot * Quaternion.Euler(leftHandRotationOffset);
        }

        transform.position += transform.TransformDirection(leftHandPositionOffset);
    }

    private void UpdateAimCrosshair()
    {
        if (aimCrosshair == null && aimLine == null) return;

        Ray ray = new Ray(nockingPoint.position, transform.right);
        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, aimMaxDistance);
        Vector3 targetPoint = hit ? hitInfo.point : ray.GetPoint(aimMaxDistance);

        if (aimCrosshair != null)
        {
            aimCrosshair.position = targetPoint;
            aimCrosshair.rotation = Quaternion.LookRotation(mainCamera.transform.position - targetPoint);
            aimCrosshair.gameObject.SetActive(true);
        }

        if (aimLine != null)
        {
            aimLine.positionCount = 2;
            aimLine.enabled = true;
            aimLine.SetPosition(0, nockingPoint.position);
            aimLine.SetPosition(1, targetPoint);
        }
    }

    private void OnTriggerPressed()
    {
        if (isDrawing) return;
        isDrawing = true;

        var rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (rightHand.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 localPos))
        {
            Transform origin = mainCamera.transform.parent;
            Vector3 rightHandWorldPos = origin != null ? origin.TransformPoint(localPos) : localPos;
            drawStartDistance = Vector3.Distance(rightHandWorldPos, nockingPoint.position);
        }

        arrowSpawner.SpawnArrow();

        if (audioSource != null && drawClip != null)
        {
            audioSource.clip = drawClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private void OnTriggerReleased()
    {
        if (!isDrawing) return;

        if (audioSource != null)
            audioSource.Stop();

        if (pullAmount > 0.1f)
        {
            if (audioSource != null && fireClip != null)
                audioSource.PlayOneShot(fireClip);
            arrowSpawner.FireArrow(pullAmount);
        }
        else
            arrowSpawner.CancelArrow();

        pullAmount = 0f;
        isDrawing = false;
        UpdateString(0f);
    }

    public void UpdateString(float pull)
    {
        if (wbStringBone == null) return;
        wbStringBone.localPosition = defaultStringPos + new Vector3(0f, 0f, pull * pullOffset);
    }
}
