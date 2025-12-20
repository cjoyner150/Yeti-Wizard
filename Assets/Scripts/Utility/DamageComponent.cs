using UnityEngine;

public class DamageComponent : MonoBehaviour
{
    [SerializeField] private float damage;

    public float Damage => damage;

    public void SetDamage(float newDamage)
    {
        damage = newDamage;
    }
}
