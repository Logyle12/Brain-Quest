using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Keeps the background music object alive across scene loads, and makes sure
// only one copy of it ever exists at a time
public class Music : MonoBehaviour
{

    // Runs as soon as this object wakes up, before anything else gets a chance to run
    private void Awake()
    {
        // Check for duplicates and decide whether this object should survive
        musicSingleton();

    }

    // Makes sure only one music object ever survives a scene change
    private void musicSingleton()
    {
        // If more than one of this component already exists in the scene, this is a duplicate
        if (FindObjectsOfType(GetType()).Length > 1)
        {
            // Get rid of the duplicate so the original keeps playing uninterrupted
            Destroy(gameObject);

        }

        else
        {

            // This is the only copy, so let it survive scene changes
            DontDestroyOnLoad(gameObject);

        }


    }

}