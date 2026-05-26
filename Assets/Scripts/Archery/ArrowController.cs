using UnityEngine;

public class ArrowController : MonoBehaviour
{
    private Rigidbody rb;
    private bool fired = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    public void Fire(Vector3 direction, float force)
    {
        rb.isKinematic=false;
        rb.AddForce(direction * force, ForceMode.Impulse);
        fired = true;
        Destroy(gameObject, 5f);
    }

    private void Update()
    {
        if(!fired || rb.velocity.sqrMagnitude < 0.1f)
        {
            return;
        }
        transform.rotation = Quaternion.LookRotation(rb.velocity);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!fired) return;

        ScoreZone zone = other.GetComponent<ScoreZone>();

        if (zone != null)
        {
            zone.RegisterHit(this);
            StickTo(other.transform);
            return;
        }

        rb.isKinematic = true;
        fired = false;
        Destroy(gameObject, 3f);
    }

    private void StickTo(Transform target)
    {
        rb.isKinematic = true;
        fired = false;
        transform.SetParent(target);
    }
}
