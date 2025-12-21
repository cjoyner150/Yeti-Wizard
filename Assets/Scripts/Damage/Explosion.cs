using UnityEngine;

public class Explosion : MonoBehaviour
{
    SphereCollider col;
    float explosionForce;
    float explosionRadius;

    private void Awake()
    {
        col = GetComponent<SphereCollider>();
    }

    public void InitExplosion(float _explosionForce, float _explosionRadius)
    {
        explosionForce = _explosionForce;
        explosionRadius = _explosionRadius;

        col.radius = explosionRadius;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawSphere(transform.position, explosionRadius);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Rigidbody rb))
        {
            rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
        }

        if ((other.CompareTag("Player") || other.CompareTag("Enemy")) && other.TryGetComponent(out IDamageable damageable))
        {
            damageable.Hit(3);
        }
    }
}
