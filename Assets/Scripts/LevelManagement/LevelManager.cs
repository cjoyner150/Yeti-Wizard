using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class LevelManager: MonoBehaviour
{
    [Header("Enemy Spawning")]
    [SerializeField] EnemySpawner[] enemySpawners;
    [SerializeField] int enemySpawnIncreaseRate;
    [SerializeField] float heavyEnemySpawnChanceIncreaseRate;
    [SerializeField] float helpfulItemSpawnChanceDecreaseRate;

    [Header("Item Spawning")]
    [SerializeField] ItemSpawner[] ItemSpawners;

    [Header("Inscribed Variables")]
    [SerializeField] float prepTime;

    [Header("Waves")]
    [SerializeField] WaveConfig initialConfig;
    WaveConfig currentConfig;

    [Header("References")]
    [SerializeField] Rigidbody player;
    [SerializeField] Transform playerStart;
    [SerializeField] PlayableDirector startWaveTimeline;
    [SerializeField] TimeStopVFX tsv;

    [Header("Events")]
    [SerializeField] VoidEventSO freezeEvent;
    [SerializeField] VoidEventSO unfreezeEvent;
    [SerializeField] VoidEventSO onWaveComplete;
    [SerializeField] VoidEventSO onEnemyDied;

    [Header("Music")]
    [SerializeField] AudioSource source;
    [SerializeField] AudioClip prepLoop01;
    [SerializeField] AudioClip prepLoop02;
    [SerializeField] AudioClip prepLoop03;
    [SerializeField] AudioClip combatLoop;

    float prepTimer;
    bool frozen = true;
    int currentAliveEnemies;

    static LevelManager instance;

    private void OnEnable()
    {
        onWaveComplete.onEventRaised += OnWaveComplete;
        onEnemyDied.onEventRaised += OnEnemyDied;
    }


    private void OnDisable()
    {
        onWaveComplete.onEventRaised -= OnWaveComplete;
        onEnemyDied.onEventRaised -= OnEnemyDied;
    }

    private void Awake()
    {
        player.position = playerStart.position;
        player.rotation = playerStart.rotation;

        if (instance != null) Destroy(this); 
        else
        {
            instance = this;
        }

        currentConfig = initialConfig;

        BeginWave();
    }

    private void Start()
    {
        Freeze();
    }

    private void Update()
    {
        if (frozen)
        {
            prepTimer -= Time.deltaTime;

            if (prepTimer <= 0)
            {
                Unfreeze();
            }
        }
    }

    private void BeginWave()
    {
        prepTimer = prepTime;

        int spawnAmountPerSpawner = currentConfig.enemies / enemySpawners.Length;
        int spawned = 0;

        foreach (var spawner in enemySpawners)
        {
            spawner.InitEnemySpawner(currentConfig, spawnAmountPerSpawner);
            spawned += spawner.SpawnEnemies();
        }

        foreach (var spawner in ItemSpawners)
        {
            spawner.InitSpawner(currentConfig);
            spawner.SpawnItems();
        }

        currentAliveEnemies = spawned;

        player.position = playerStart.position;
        player.rotation = playerStart.rotation;
    }

    void OnWaveComplete()
    {
        currentConfig = GenerateNextWave();
        BeginWave();
        Freeze();
    }

    WaveConfig GenerateNextWave()
    {
        int newWaveNumber = currentConfig.waveNumber + 1;
        int newEnemyAmount = (newWaveNumber * enemySpawnIncreaseRate) + 20;
        int newEnemyDamageMultiplier;

        if (newWaveNumber < 3)
        {
            newEnemyDamageMultiplier = 1;
        }
        else if (newWaveNumber < 10)
        {
            newEnemyDamageMultiplier = 2;
        }
        else newEnemyDamageMultiplier = 3;

        float newHeavyEnemySpawnChance = (newWaveNumber * heavyEnemySpawnChanceIncreaseRate) + .1f;
        float newHelpfulItemSpawnChance = 1f - (newWaveNumber * helpfulItemSpawnChanceDecreaseRate);

        return new WaveConfig(newWaveNumber, newEnemyAmount, newEnemyDamageMultiplier, newHeavyEnemySpawnChance, newHelpfulItemSpawnChance);

    }

    private void Freeze()
    {
        freezeEvent.onEventRaised?.Invoke();
        frozen = true;
        tsv.setTimeStop(true);

        source.loop = false;
        source.Stop();
        StartCoroutine(MusicPrep());
    }

    IEnumerator MusicPrep()
    {
        source.PlayOneShot(prepLoop01);

        yield return new WaitForSecondsRealtime(60);

        source.PlayOneShot(prepLoop03);

        yield return new WaitForSecondsRealtime(60);
    }

    private void Unfreeze()
    {
        frozen = false;
        unfreezeEvent.onEventRaised?.Invoke();
        tsv.setTimeStop(true);

        source.loop = true;
        source.clip = combatLoop;
        source.Play();
    }

    private void OnEnemyDied()
    {
        currentAliveEnemies--;

        if (currentAliveEnemies <= 0)
        {
            onWaveComplete.onEventRaised?.Invoke();
        }
    }

    public int GetWave()
    {
        return currentConfig.waveNumber;
    }
}
