using UnityEngine;

public class HelpfulItemSpawner : MonoBehaviour
{
    [SerializeField] GameObject[] helpfulItemPrefabs;
    [SerializeField] Transform[] helpfulItemSpawnLocations;

    float currentSpawnChance = 1;

    public void InitSpawner(WaveConfig config)
    {
        currentSpawnChance = config.helpfulItemSpawnChance;
    }

    public void SpawnItems()
    {
        foreach (var loc in helpfulItemSpawnLocations)
        {
            float rand = Random.value;
            if (rand < currentSpawnChance)
            {
                GameObject go = Instantiate(helpfulItemPrefabs[Random.Range(0, helpfulItemPrefabs.Length)], loc.position, loc.rotation);

            }


        }
    }
}
