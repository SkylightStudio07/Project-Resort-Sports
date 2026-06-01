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

    [Header("Camera Attach Settings")]
    [SerializeField] private Vector3 attachOffset = new Vector3(0.2f, -0.3f, 0.5f);
    [SerializeField] private Vector3 attachRotation = new Vector3(0f, -80f, 0f);

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

    private void Update()
    {
        if (!isHeld) return;

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
            pullAmount = Mathf.Clamp01(distance / maxPullDistance);
            UpdateString(pullAmount);
            rightHand.SendHapticImpulse(0, pullAmount * maxHapticAmplitude, Time.deltaTime);
        }
    }

    private void OnTriggerPressed()
    {
        if (isDrawing) return;
        isDrawing = true;
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
