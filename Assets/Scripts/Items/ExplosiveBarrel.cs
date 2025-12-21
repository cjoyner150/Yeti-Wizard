using UnityEngine;

public class ExplosiveBarrel : DraggableItem
{
    [Header("Explosive")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private GameObject explosionVFXPrefab;
    [SerializeField] float explosionRadius;
    [SerializeField] float explosionForce;
    

    public override void Die()
    {
        if (currentState == DraggableState.shattered) return;

        GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        GameObject vfx = Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);

        Explosion explo = explosion.GetComponent<Explosion>();
        explo.InitExplosion(explosionForce, explosionRadius);

        Destroy(vfx, 3f);
        Destroy(gameObject);

        base.Die();
    }
}
