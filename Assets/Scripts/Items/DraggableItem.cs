using UnityEngine;

public class DraggableItem : MonoBehaviour, IDamageable
{
    [SerializeField] protected MeshRenderer mesh;
    [SerializeField] protected Collider[] colliders;
    [SerializeField] protected float velocityThreshold = 10;
    [SerializeField] protected float yOffset = 0;

    Rigidbody rb;

    public int Health { get; set; }

    public virtual void InitItem()
    {
        transform.position += new Vector3(0, yOffset, 0);
        rb = GetComponent<Rigidbody>();

        LockItem();
    }

    public virtual void PickUpItem()
    {
        rb.isKinematic = false;
        rb.useGravity = false;
    }

    public virtual void LockItem()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    public virtual void UnlockItem()
    {
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    public virtual void Die()
    {
        mesh.enabled = false;

        foreach (Collider collider in colliders) collider.enabled = false;
    }

    public virtual void Hit(int damage)
    {
        Health -= damage;

        if (Health <= 0) Die();
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude > velocityThreshold)
        {
            Hit(1);
        }
    }
}
