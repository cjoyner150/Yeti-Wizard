using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;

public class TimeStopPostProcess_InputSystem : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Key used to toggle time stop")]
    public Key toggleKey = Key.T;

    [Header("Transition")]
    public float transitionTime = 0.25f;

    [Header("Time Stop Values")]
    public float stoppedSaturation = -70f;
    public float stoppedContrast = 20f;
    public float stoppedBloom = 1.2f;
    public float stoppedVignette = 0.45f;
    public float stoppedChromatic = 0.25f;

    Volume volume;

    ColorAdjustments color;
    Bloom bloom;
    Vignette vignette;
    ChromaticAberration chromatic;

    bool timeStopped = false;
    float t = 1f;

    // Normal values
    float normalSaturation;
    float normalContrast;
    float normalBloom;
    float normalVignette;
    float normalChromatic;

    InputAction toggleAction;

    void Awake()
    {
        // Inline InputAction
        toggleAction = new InputAction(
            "ToggleTimeStop",
            InputActionType.Button,
            binding: $"<Keyboard>/{toggleKey.ToString().ToLower()}"
        );
    }

    void OnEnable()
    {
        toggleAction.Enable();
    }

    void OnDisable()
    {
        toggleAction.Disable();
    }

    void Start()
    {
        volume = FindObjectOfType<Volume>();

        if (!volume)
        {
            Debug.LogError("No Volume found in scene.");
            enabled = false;
            return;
        }

        volume.profile.TryGet(out color);
        volume.profile.TryGet(out bloom);
        volume.profile.TryGet(out vignette);
        volume.profile.TryGet(out chromatic);

        normalSaturation = color.saturation.value;
        normalContrast = color.contrast.value;
        normalBloom = bloom.intensity.value;
        normalVignette = vignette.intensity.value;
        normalChromatic = chromatic.intensity.value;

        timeStopped = true;
    }

    void Update()
    {
        if (toggleAction.WasPressedThisFrame())
        {
            timeStopped = !timeStopped;
            Time.timeScale = timeStopped ? 0f : 1f;
            t = 0f;
            Debug.Log("Time Change!");
        }

        AnimatePostProcess();
    }

    void AnimatePostProcess()
    {
        if (t >= 1f)
            return;

        t += Time.unscaledDeltaTime / transitionTime;
        t = Mathf.Clamp01(t);

        float blend = timeStopped ? t : 1f - t;

        color.saturation.value = Mathf.Lerp(normalSaturation, stoppedSaturation, blend);
        color.contrast.value = Mathf.Lerp(normalContrast, stoppedContrast, blend);
        bloom.intensity.value = Mathf.Lerp(normalBloom, stoppedBloom, blend);
        vignette.intensity.value = Mathf.Lerp(normalVignette, stoppedVignette, blend);
        chromatic.intensity.value = Mathf.Lerp(normalChromatic, stoppedChromatic, blend);
    }
}
