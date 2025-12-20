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

    private void Start()
    {
        disappearTimer = disappearTime;
    }

    private void Update()
    {
        if (disappearTimer > 0)
        {
            disappearTimer -= Time.deltaTime;
            return;
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        Destroy(gameObject);
    }

    public void Init(float damage, float launchSpeed)
    {
        speed = launchSpeed;
        damageComponent.SetDamage(damage);
    }

    public void Launch()
    {
        rb.AddForce(transform.forward * speed, ForceMode.Impulse);
    }
}
