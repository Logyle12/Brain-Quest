using UnityEngine;

// This forces Unity to automatically add an AudioSource to the GameObject
[RequireComponent(typeof(AudioSource))]
public class ButtonSFX : MonoBehaviour
{
    // Reference to the AudioSource this button plays its click sound through
    private AudioSource audioSource;

    private void Start()
    {
        // Cache the reference so we don't call GetComponent every click
        audioSource = GetComponent<AudioSource>();
    }

    // Called when the button is clicked, plays the assigned click sound
    public void ButtonClickedSound()
    {
        // Only play if a clip has actually been assigned in the Inspector
        if (audioSource.clip != null)
        {
            // PlayOneShot rather than Play, so rapid repeated clicks overlap
            // instead of cutting each other off
            audioSource.PlayOneShot(audioSource.clip);
        }
    }
}