using UnityEngine;

public class DraggableItem : MonoBehaviour, IDamageable
{
    [SerializeField] protected MeshRenderer mesh;
    [SerializeField] protected Collider[] colliders;
    [SerializeField] protected float velocityThreshold = 10;
    [SerializeField] protected float yOffset = 0;
    [SerializeField] protected VoidEventSO freezeEvent;
    [SerializeField] protected VoidEventSO unfreezeEvent;

    protected Rigidbody rb;
    protected bool frozen;

    public DraggableState currentState;
    public enum DraggableState
    {
        frozen,
        unfrozen,
        dragging
    }

    public int Health { get; set; }

    void OnEnable()
    {
        freezeEvent.onEventRaised += TryFreezeItem;
        unfreezeEvent.onEventRaised += TryUnfreezeItem;
    }

    void OnDisable()
    {
        freezeEvent.onEventRaised -= TryFreezeItem;
        unfreezeEvent.onEventRaised -= TryUnfreezeItem;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public virtual void InitItem()
    {
        transform.position += new Vector3(0, yOffset, 0);

        FreezeItem();
    }

    public virtual void PickUpItem()
    {
        rb.isKinematic = false;
        rb.useGravity = false;

        currentState = DraggableState.dragging;
    }

    public virtual void DropItem()
    {
        if (frozen)
        {
            FreezeItem();
        }
        else
        {
            UnfreezeItem();
        }
    }

    void TryFreezeItem()
    {
        Debug.Log($"{name} recieved freeze event");

        if (currentState == DraggableState.unfrozen)
        {
            FreezeItem();
        }
        else frozen = true;
    }

    void TryUnfreezeItem()
    {
        Debug.Log($"{name} recieved unfreeze event");

        if (currentState == DraggableState.frozen)
        {
            UnfreezeItem();
        }
        else frozen = false;
    }

    public virtual void FreezeItem()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.useGravity = false;

        frozen = true;

        currentState = DraggableState.frozen;
    }

    public virtual void UnfreezeItem()
    {
        rb.isKinematic = false;
        rb.useGravity = true;

        frozen = false;

        currentState = DraggableState.unfrozen;
    }

    public virtual void Die()
    {
        Destroy(gameObject);

        //foreach (Collider collider in colliders) collider.enabled = false;
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
