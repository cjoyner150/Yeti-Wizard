using UnityEngine;

public interface IDamageable
{
    public int Health { get; set; }
    public void Hit(int damage);
    public void Die();
}
