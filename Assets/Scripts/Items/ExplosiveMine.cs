using UnityEngine;

public class ExplosiveMine : ExplosiveBarrel
{
    bool triggered = false;

    [SerializeField] Collider trapTrigger;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || currentState == DraggableState.shattered) return;

        if (other.CompareTag("Player") || other.CompareTag("Enemy"))
        {
            triggered = true;

            Die();
        }
    }

    public override void Die()
    {
        if (currentState == DraggableState.shattered) return;

        trapTrigger.enabled = false;

        base.Die();
    }
}
