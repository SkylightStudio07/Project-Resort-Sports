using UnityEngine;

public class ArcheryExitGate : MonoBehaviour
{
    [SerializeField] private BowController bow;
    [SerializeField] private ArcheryGameManager gameManager;
    [SerializeField] private ArcheryUI ui;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<CharacterController>() == null) return;

        bow?.ResetState();
        gameManager?.StopGame();
        ui?.HideAll();
    }
}
