using UnityEngine;

public class AnimationSound : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip soundClip;

    public void PlayAnimationSound()
    {
        if (audioSource == null || soundClip == null)
        {
            Debug.LogWarning("AudioSource or AudioClip is missing.");
            return;
        }

        audioSource.PlayOneShot(soundClip);
    }
}
