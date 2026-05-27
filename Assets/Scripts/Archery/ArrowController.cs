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
        rb.isKinematic = false;
        rb.AddForce(direction * force, ForceMode.Impulse);
        fired = true;
    }

    private void Update()
    {
        if(!fired || rb.velocity.sqrMagnitude < 0.1f)
        {
            return;
        }
        transform.rotation = Quaternion.LookRotation(rb.velocity);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!fired) return;

        TargetHitDetector target = collision.gameObject.GetComponent<TargetHitDetector>();

        if (target != null)
        {
            target.RegisterHit(transform.position);
            StickTo(collision.transform);
            return;
        }

        rb.isKinematic = true;
        fired = false;
        Destroy(gameObject, 5f);
    }

    private void StickTo(Transform target)
    {
        rb.isKinematic = true;
        fired = false;
        transform.SetParent(target);
    }
}
