using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] GameObject enemyPrefab;
    [SerializeField] GameObject heavyEnemyPrefab;

    [SerializeField] Transform[] spawnLocations;
    [SerializeField] GameObject player;

    WaveConfig currentConfig;
    float heavyEnemyChance;
    int enemyAmount;

    public void InitEnemySpawner(WaveConfig config, int amount)
    {
        currentConfig = config;
        enemyAmount = amount;

        heavyEnemyChance = currentConfig.heavyEnemySpawnChance;
    }

    /// <summary>
    /// Spawns enemies with some randomness at predetermined spawn points.
    /// </summary>
    /// <returns>Amount of enemies successfully spawned and initialized.</returns>
    public int SpawnEnemies()
    {
        int count = 0;

        while (count < enemyAmount)
        {
            foreach (Transform t in spawnLocations)
            {
                float rand = Random.value;

                if (rand <= heavyEnemyChance)
                {
                    GameObject go = Instantiate(heavyEnemyPrefab, t.position, t.rotation);
                    //Enemy enemy = go.GetComponent<Enemy>();
                    //enemy.Init(player.transform, currentConfig.enemyDamageMultiplier);
                    count++;
                }
                else
                {
                    GameObject go = Instantiate(enemyPrefab, t.position, t.rotation);
                    //Enemy enemy = go.GetComponent<Enemy>();
                    //enemy.Init(player.transform, currentConfig.enemyDamageMultiplier);
                    count++;
                }
            }
        }

        return count;
    }


}
