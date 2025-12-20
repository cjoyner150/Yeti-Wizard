using System;
using UnityEngine;

[Serializable]
public class WaveConfig
{
    public int waveNumber = 0;
    public int enemies = 20;
    public int enemyDamageMultiplier = 1;
    public float heavyEnemySpawnChance = .1f;
    public float helpfulItemSpawnChance = 1f;

    public WaveConfig(int _waveNumber = 0, int _enemies = 20, int _enemyDamageMult = 1, float _heavyEnemySpawnChance = .1f, float _helpfulItemChance = 1f)
    {
        waveNumber = _waveNumber;
        enemyDamageMultiplier = _enemyDamageMult;
        enemies = _enemies;
        heavyEnemySpawnChance = _heavyEnemySpawnChance;
        helpfulItemSpawnChance = _helpfulItemChance;
    }
}
