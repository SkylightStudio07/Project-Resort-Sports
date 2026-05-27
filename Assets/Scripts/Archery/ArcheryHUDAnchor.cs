using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class ArcheryHUDAnchor : MonoBehaviour
{
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0.1f, 2f);
    [SerializeField] private float canvasScale = 0.001f;

    private void Start()
    {
        if (Camera.main == null) return;

        transform.SetParent(Camera.main.transform);
        transform.localPosition = localOffset;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one * canvasScale;
    }
}
