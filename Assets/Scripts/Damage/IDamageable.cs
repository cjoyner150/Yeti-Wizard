using UnityEngine;

public interface IDamageable
{
    float Health { get; }

    void Damage(float damage);
}