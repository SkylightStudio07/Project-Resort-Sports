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
        {
            Destroy (currentArrow);
        }
        currentArrow = Instantiate(arrowPrefab, nockingPoint.position, nockingPoint.rotation);
    }

    public void FireArrow(float pullAmount)
    {
        if (currentArrow == null) return;
        if (gameManager.State != ArcheryGameManager.GameState.Playing) return;

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
            currentArrow.transform.SetPositionAndRotation(nockingPoint.position, nockingPoint.rotation);
        }
    }
}