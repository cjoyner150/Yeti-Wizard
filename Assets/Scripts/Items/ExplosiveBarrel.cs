using UnityEngine;

public class ExplosiveBarrel : DraggableItem
{
    [Header("Explosive")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private GameObject explosionVFXPrefab;

    public override void Die()
    {
        if (currentState == DraggableState.shattered) return;

        Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        GameObject vfx = Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);

        Destroy(vfx, 3f);
        Destroy(gameObject);

        base.Die();
    }
}
