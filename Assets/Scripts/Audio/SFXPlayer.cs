using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SFXPlayer : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] private AudioClip[] clips;

    private AudioSource audioSource;

    private void Awake()
    {
        TryGetComponent(out audioSource);
    }

    public void Play(bool playOneShot = true, bool interruptExistingSound = true)
    {
        if (clips == null) return;
        if (clips.Length == 0) return;

        AudioClip clipToPlay = clips[Random.Range(0, clips.Length)];

        if (audioSource == null)
        {
            if (!TryGetComponent(out audioSource)) return;
            if (!audioSource.isActiveAndEnabled) return;
        }

        if (clipToPlay == null) return;

        if (playOneShot)
        {
            audioSource.PlayOneShot(clipToPlay);
            return;
        }

        if (!interruptExistingSound)
        {
            if (audioSource.isPlaying) return;
        }

        audioSource.Stop();
        audioSource.clip = clipToPlay;
        audioSource.Play();
    }

    public void PlayClipAtPoint()
    {
        if (clips == null) return;
        if (clips.Length == 0) return;

        AudioClip clipToPlay = clips[Random.Range(0, clips.Length)];

        if (clipToPlay == null) return;

        AudioSource.PlayClipAtPoint(clipToPlay, transform.position, 1);
    }
}
