using TMPro;
using UnityEngine;

public class ResultsDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI waveCountTextBox;

    private void Start()
    {
        DisplayWaveCount();
    }

    private void DisplayWaveCount()
    {
        if (waveCountTextBox == null || PersistentResultsStorage.Instance == null) return;
        
        waveCountTextBox.text = "Waves Completed: " + PersistentResultsStorage.Instance.WaveCount;
    }
}
