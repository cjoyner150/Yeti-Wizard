using UnityEngine;
using UnityEngine.SceneManagement;

public class Clock : MonoBehaviour, IDamageable
{
    public int Health { get; set; }
    bool dead = false;

    void Awake()
    {
        Health = 10;
    }

    public void Die()
    {
        dead = true;
        SceneManager.LoadScene(2);
    }

    public void Hit(int damage)
    {
        if (dead) return;

        Health -= damage;

        if (Health <= 0) Die();
    }
}
