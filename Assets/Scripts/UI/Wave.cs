using TMPro;
using UnityEngine;

public class Wave : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI tmp;
    [SerializeField] LevelManager lm;
    void Update()
    {
        tmp.text = "Wave: " + lm.GetWave();
    }
}
