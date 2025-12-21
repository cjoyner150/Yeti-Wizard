using UnityEngine;

public class EnemyRagdollPart : MonoBehaviour
{
    public float DamageMult { get; set; }
    public float DamageVelocityThreshold { get; set; }

    private const string HIT_METHOD_NAME = "HitFromPart";

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody hitRigidbody = collision.rigidbody;
        if (hitRigidbody != null)
        {
            if (hitRigidbody.transform.IsChildOf(transform)) return;
            if (transform.GetComponentInParent<Enemy>() && hitRigidbody.transform.IsChildOf(transform.GetComponentInParent<Enemy>().transform)) return;
            if (hitRigidbody.GetComponentInParent<Enemy>() && transform.IsChildOf(hitRigidbody.GetComponentInParent<Enemy>().transform)) return;

            int damage;
            if (!hitRigidbody.TryGetComponent(out DamageComponent dmgComponent))
            {
                if (hitRigidbody.linearVelocity.magnitude < DamageVelocityThreshold) return;

                damage = Mathf.RoundToInt(hitRigidbody.linearVelocity.magnitude * DamageMult);
            }
            else damage = dmgComponent.Damage;

            SendMessageUpwards(HIT_METHOD_NAME, damage);
            return;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out DamageComponent dmgComponent))
        {
            SendMessageUpwards(HIT_METHOD_NAME, dmgComponent.Damage);
        }
    }
}
