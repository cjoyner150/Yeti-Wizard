using UnityEngine;

public class ExplosiveBarrel : DraggableItem
{
    [SerializeField] private GameObject explosionPrefab;

    public override void Die()
    {
        Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        base.Die();
    }
}
