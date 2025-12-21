using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;

public class TimeStopVFX : MonoBehaviour
{
    

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


    void Start()
    {

        volume = FindObjectOfType<Volume>();
        volume.profile = Instantiate(volume.profile);

        if (!volume)
        {
            Debug.LogError("No Volume found in scene.");
            return;
        }

        if (!volume.profile.TryGet(out color))
            Debug.LogWarning("No ColorAdjustments found");
        if (!volume.profile.TryGet(out bloom))
            Debug.LogWarning("No Bloom found");
        if (!volume.profile.TryGet(out vignette))
            Debug.LogWarning("No Vignette found");
        if (!volume.profile.TryGet(out chromatic))
            Debug.LogWarning("No ChromaticAberration found");

        normalSaturation = color.saturation.value;
        normalContrast = color.contrast.value;
        normalBloom = bloom.intensity.value;
        normalVignette = vignette.intensity.value;
        normalChromatic = chromatic.intensity.value;

        timeStopped = true;
    }

    void Update()
    {
        AnimatePostProcess();
    }

    public void setTimeStop(bool ts)
    {
        timeStopped = ts;
        Debug.Log("Time vfx = " + ts);
        t = 0;
    }

    void AnimatePostProcess()
    {
        if (t >= 1f)
            return;

        t += Time.deltaTime / transitionTime;
        t = Mathf.Clamp01(t);

        float blend = timeStopped ? t : 1f - t;

        color.saturation.value = Mathf.Lerp(normalSaturation, stoppedSaturation, blend);
        color.contrast.value = Mathf.Lerp(normalContrast, stoppedContrast, blend);
        bloom.intensity.value = Mathf.Lerp(normalBloom, stoppedBloom, blend);
        vignette.intensity.value = Mathf.Lerp(normalVignette, stoppedVignette, blend);
        chromatic.intensity.value = Mathf.Lerp(normalChromatic, stoppedChromatic, blend);


        Debug.Log($"Saturation: {color.saturation.value}, Bloom: {bloom.intensity.value}");
    }
}
