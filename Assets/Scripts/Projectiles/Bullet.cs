using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(DamageComponent))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private float disappearTime;

    private float speed;
    private float disappearTimer;

    private Rigidbody rb;
    private DamageComponent damageComponent;

    private void Awake()
    {
        TryGetComponent(out rb);
        TryGetComponent(out damageComponent);
    }

    private void Update()
    {
        if (disappearTimer > 0)
        {
            disappearTimer -= Time.deltaTime;

            if (disappearTimer <= 0) Destroy(gameObject);
            return;
        }
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    IDamageable damageable = other.GetComponentInParent<IDamageable>();
    //    damageable?.Hit(damageComponent.Damage);
    //    Destroy(gameObject);
    //}

    public void Init(int damage, float launchSpeed)
    {
        speed = launchSpeed;
        damageComponent.SetDamage(damage);
    }

    public void Launch()
    {
        rb.AddForce(transform.forward * speed, ForceMode.Impulse);
        disappearTimer = disappearTime;
    }
}
