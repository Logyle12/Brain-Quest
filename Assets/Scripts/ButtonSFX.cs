using UnityEngine;

// This forces Unity to automatically add an AudioSource to the GameObject
[RequireComponent(typeof(AudioSource))]
public class ButtonSFX : MonoBehaviour
{
    private AudioSource audioSource;

    private void Start()
    {
        // Cache the reference
        audioSource = GetComponent<AudioSource>();
    }

    public void ButtonClickedSound()
    {
        // The clip now lives on the AudioSource itself (assign it in the Inspector),
        // instead of being duplicated as a separate field on this script.
        if (audioSource.clip != null)
        {
            // PlayOneShot (rather than Play) so rapid repeated clicks overlap
            // instead of cutting each other off.
            audioSource.PlayOneShot(audioSource.clip);
        }
    }
}