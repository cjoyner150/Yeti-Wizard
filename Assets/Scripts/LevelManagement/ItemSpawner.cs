using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField] GameObject[] ItemPrefabs;
    [SerializeField] Transform[] ItemSpawnLocations;
    [SerializeField] bool helpfulItems;

    float currentSpawnChance = 1;

    public void InitSpawner(WaveConfig config)
    {
        currentSpawnChance = config.helpfulItemSpawnChance;
    }

    public void SpawnItems()
    {
        foreach (var loc in ItemSpawnLocations)
        {
            float rand = Random.value;
            if (rand < currentSpawnChance || !helpfulItems)
            {
                GameObject go = Instantiate(ItemPrefabs[Random.Range(0, ItemPrefabs.Length)], loc.position, loc.rotation);

                DraggableItem[] draggables = go.GetComponentsInChildren<DraggableItem>();
                foreach(var draggable in draggables) draggable.InitItem();
            }
        }
    }
}
