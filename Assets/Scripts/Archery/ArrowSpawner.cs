using UnityEngine;

public class ArrowSpawner : MonoBehaviour
{
    [SerializeField]
    private ArcheryGameManager gameManager;

    [Header("Settings")]
    [SerializeField]
    private GameObject arrowPrefab;
    [SerializeField]
    private Transform nockingPoint;
    [SerializeField]
    private float maxForce = 30f;

    private GameObject currentArrow;

    public void SpawnArrow()
    {
        if (currentArrow != null)
            Destroy(currentArrow);

        Quaternion rot = Quaternion.LookRotation(Camera.main.transform.forward) * Quaternion.Euler(90f, 0f, 0f);
        currentArrow = Instantiate(arrowPrefab, nockingPoint.position, rot);
    }

    public void FireArrow(float pullAmount)
    {
        if (currentArrow == null) return;
        if (gameManager.State != ArcheryGameManager.GameState.Playing) return;

        var arrowColliders = currentArrow.GetComponentsInChildren<Collider>();
        var bowColliders = GetComponentsInChildren<Collider>();
        foreach (var ac in arrowColliders)
            foreach (var bc in bowColliders)
                Physics.IgnoreCollision(ac, bc);

        var arrow = currentArrow.GetComponent<ArrowController>();
        arrow.Fire(Camera.main.transform.forward, pullAmount * maxForce);
        currentArrow = null;

        gameManager.OnArrowFired();
    }

    public void CancelArrow()
    {
        if (currentArrow != null)
        {
            Destroy (currentArrow);
        }
        currentArrow = null;
    }

    private void Update()
    {
        if (currentArrow != null)
        {
            Quaternion rot = Quaternion.LookRotation(Camera.main.transform.forward) * Quaternion.Euler(90f, 0f, 0f);
            currentArrow.transform.SetPositionAndRotation(nockingPoint.position, rot);
        }
    }
}