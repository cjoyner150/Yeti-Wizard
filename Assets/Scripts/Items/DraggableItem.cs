using System.Collections;
using UnityEngine;

public class DraggableItem : MonoBehaviour, IDamageable
{
    [Header("Destructible")]
    [SerializeField] protected Collider[] shatterColliders;
    [SerializeField] protected Transform shatterExplosionPoint;
    [SerializeField] protected float shatterExplosionForce;
    [SerializeField] protected float shatterExplosionRange;
    [SerializeField] protected bool isDestructible;

    [Header("Draggable")]
    [SerializeField] protected float impulseDamageThreshold = 10;
    [SerializeField] protected float yOffset = 0;
    [SerializeField] protected int maxHP = 1;
    [SerializeField] protected VoidEventSO freezeEvent;
    [SerializeField] protected VoidEventSO unfreezeEvent;
    [SerializeField] GameObject lightningPrefab;

    [Header("Audio")]
    [SerializeField] private SFXPlayer destroyedSFXPlayer;

    [SerializeField] GameObject shatterParticlePrefab;

    GameObject lightningParticle;

    private Quaternion rot;
    private ContactPoint contact;

    protected Rigidbody rb;
    protected Collider col;
    protected bool frozen;

    [Header("State")]
    public DraggableState currentState;
    public enum DraggableState
    {
        frozen,
        unfrozen,
        dragging,
        shattered
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

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        if (isDestructible)
        {
            foreach (var collider in shatterColliders)
            {
                collider.enabled = false;
            }
        }

        Health = maxHP;
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
        if (currentState == DraggableState.shattered) return;

        Instantiate(shatterParticlePrefab, contact.point, rot);

        if (isDestructible)
        {
            rb.isKinematic = true;
            rb.useGravity = false;

            col.enabled = false;

            foreach (var collider in shatterColliders)
            {
                collider.enabled = true;
                collider.gameObject.transform.SetParent(null, true);
                Rigidbody rigidBody = collider.gameObject.AddComponent<Rigidbody>();

                rigidBody.isKinematic = false;
                rigidBody.useGravity = true;
                rigidBody.interpolation = RigidbodyInterpolation.Interpolate;
                rigidBody.collisionDetectionMode = CollisionDetectionMode.Continuous;

                rigidBody.AddExplosionForce(shatterExplosionForce, shatterExplosionPoint.position, shatterExplosionRange);
            }

            currentState = DraggableState.shattered;

            //Debug.Log($"{name} has died");
        }
        else
        {
            if (destroyedSFXPlayer != null) destroyedSFXPlayer.PlayClipAtPoint();
            Destroy(gameObject);
        }
    }

    public void SetPickupVFX(bool isOn)
    {
        float str = isOn ? 2f : 0f;

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer renderer in renderers)
        {
            renderer.material.SetFloat("_Highlight_Strength", str);
        }
           

        if (isOn)
        {
            lightningParticle = Instantiate(lightningPrefab, transform.position, Quaternion.identity, transform);
        }
        else
        {
            Destroy(lightningParticle);
        }
    }

    public virtual void Hit(int damage)
    {
        if (currentState == DraggableState.shattered) return;

        Health -= damage;

        if (Health <= 0) Die();
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        contact = collision.contacts[0];
        Vector3 normal = contact.normal;
        rot = Quaternion.LookRotation(normal);



        Debug.Log($"{name} was hit with {collision.impulse.magnitude} impulse");
        if (collision.impulse.magnitude > impulseDamageThreshold && !frozen)
        {
            Hit(1);
        }
    }
}
