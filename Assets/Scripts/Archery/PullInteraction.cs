using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class PullInteraction : MonoBehaviour
{
    [Header("Pull Settings")]
    [SerializeField] private float maxPullDistance = 0.5f;

    [Header("Haptic")]
    [SerializeField] private float maxHapticAmplitude = 0.6f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip drawClip;
    [SerializeField] private AudioClip fireClip;

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

        if (interactable == null)
        {
            enabled = false;
            return;
        }

        interactable.selectEntered.AddListener(OnStartPull);
        interactable.selectExited.AddListener(OnStopPull);
    }

    private void OnStartPull(SelectEnterEventArgs args)
    {
        if (!bow.isHeld) return;

        pullingHand = args.interactorObject.transform;
        isPulling = true;
        bow.activePull = this;
        arrowSpawner.SpawnArrow();

        if (audioSource != null && drawClip != null)
        {
            audioSource.clip = drawClip;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private void OnStopPull(SelectExitEventArgs args)
    {
        if (audioSource != null)
            audioSource.Stop();

        if (pullAmount > 0.1f)
        {
            if (audioSource != null && fireClip != null)
                audioSource.PlayOneShot(fireClip);
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
        bow.UpdateString(0f);
    }

    private void Update()
    {
        if (!isPulling || pullingHand == null) return;

        float distance = Vector3.Distance(pullingHand.position, bow.NockingPoint.position);
        pullAmount = Mathf.Clamp01(distance / maxPullDistance);
        bow.UpdateString(pullAmount);

        // 당기는 강도에 비례해서 진동 세기 증가
        var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        device.SendHapticImpulse(0, pullAmount * maxHapticAmplitude, Time.deltaTime);
    }
}
