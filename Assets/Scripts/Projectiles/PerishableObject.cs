using UnityEngine;

public class PerishableObject : MonoBehaviour
{
    [SerializeField] private float despawnTime = 1f;
    private float despawnTimer;

    private void Start()
    {
        despawnTimer = despawnTime;
    }

    private void Update()
    {
        if (despawnTimer > 0)
        {
            despawnTimer -= Time.deltaTime;
            return;
        }

        Destroy(gameObject);
    }
}
