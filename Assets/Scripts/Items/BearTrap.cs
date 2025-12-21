using UnityEngine;

public class BearTrap : DraggableItem
{
    Animator anim;
    bool triggered = false;

    protected override void Awake()
    {
        base.Awake();
        anim = GetComponent<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || currentState == DraggableState.shattered) return;

        if (other.CompareTag("Player") || other.CompareTag("Enemy"))
        {
            triggered = true;

            anim.SetTrigger("TriggerTrap");
            IDamageable damageable = other.GetComponent<IDamageable>();
            damageable?.Hit(2);
        }
    }
}
