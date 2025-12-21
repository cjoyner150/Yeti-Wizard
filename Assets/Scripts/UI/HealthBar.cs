using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("Listen Events")]
    [SerializeField] private IntEventSO healthSetEventSO;
    [SerializeField] private IntEventSO healthUpdatedEventSO;

    private Slider slider;

    private void Awake()
    {
        slider = GetComponentInChildren<Slider>();
    }

    private void OnEnable()
    {
        healthSetEventSO.onEventRaised += InitializeBar;
        healthUpdatedEventSO.onEventRaised += UpdateHealth;
    }

    private void OnDisable()
    {
        healthSetEventSO.onEventRaised -= InitializeBar;
        healthUpdatedEventSO.onEventRaised -= UpdateHealth;
    }

    private void InitializeBar(int value)
    {
        slider.value = slider.maxValue = value;
    }

    private void UpdateHealth(int value)
    {
        slider.value = value;

        if (value <= 0)
        {
            SceneManager.LoadScene("LoseScreen");
        }
    }
}
