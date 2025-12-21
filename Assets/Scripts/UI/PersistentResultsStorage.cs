using System;
using UnityEngine;

public class PersistentResultsStorage : MonoBehaviour
{
    public static PersistentResultsStorage Instance { get; private set; }

    [Header("Listen Events")]
    [SerializeField] private VoidEventSO waveCompleteEvent;
    private int waveCount;

    public int WaveCount => waveCount;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(Instance.gameObject);
            Instance = this;
        }
    }

    private void OnEnable()
    {
        waveCompleteEvent.onEventRaised += IncreaseWaveCount;
    }

    private void OnDisable()
    {
        waveCompleteEvent.onEventRaised -= IncreaseWaveCount;
    }

    private void IncreaseWaveCount()
    {
        waveCount++;
    }
}
