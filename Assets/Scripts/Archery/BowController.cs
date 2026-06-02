using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BowController : MonoBehaviour
{
    [SerializeField] private Transform nockingPoint;
    [SerializeField] private Transform wbStringBone;
    [SerializeField] private float pullOffset = 0.05f;

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
    private bool prevTriggerDown = false;
    private float pullAmount = 0f;
    private float peakPullAmount = 0f;

    // 드로 시작 임계값 — 살짝 누르면 장전
    private const float DrawStartThreshold = 0.1f;
    // 발사 임계값 — 절반 이하로 내려가면 즉시 발사 (딜레이 제거)
    private const float DrawReleaseThreshold = 0.5f;

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

        bool triggerDown = triggerValue > DrawStartThreshold;

        // 드로 시작
        if (triggerDown && !prevTriggerDown)
            OnTriggerPressed();

        // 발사: 드로 중 트리거가 절반 이하로 내려오면 즉시 발사
        if (isDrawing && triggerValue < DrawReleaseThreshold && prevTriggerDown)
            OnTriggerReleased();

        prevTriggerDown = triggerDown;

        if (isDrawing)
        {
            pullAmount = triggerValue;
            peakPullAmount = Mathf.Max(peakPullAmount, pullAmount);
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
        peakPullAmount = 0f;
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

        if (peakPullAmount > DrawStartThreshold)
        {
            if (audioSource != null && fireClip != null)
                audioSource.PlayOneShot(fireClip);
            arrowSpawner.FireArrow(peakPullAmount);
        }
        else
            arrowSpawner.CancelArrow();

        pullAmount = 0f;
        peakPullAmount = 0f;
        isDrawing = false;
        UpdateString(0f);
    }

    public void UpdateString(float pull)
    {
        if (wbStringBone == null) return;
        wbStringBone.localPosition = defaultStringPos + new Vector3(0f, 0f, pull * pullOffset);
    }
}
