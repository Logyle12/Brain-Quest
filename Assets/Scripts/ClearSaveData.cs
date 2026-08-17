using UnityEngine;

// Dev/debug tool, attach to any object and check the box to wipe all
// saved PlayerPrefs data when the game starts
public class ClearSaveData : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("If checked, all PlayerPrefs will be deleted when the game starts.")]
    // Toggle in the Inspector, off by default so save data doesn't get wiped by accident
    public bool clearDataOnStart = false;

    void Start()
    {
        // Only wipe data if the toggle is switched on
        if (clearDataOnStart)
        {
            // Deletes every saved key, not just ones from this game's own keys
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            
            // Loud warning so it's obvious in the console this happened
            Debug.Log("<color=red><b>[ClearSaveData]</b> PlayerPrefs have been cleared!</color>");
        }
        else
        {
            // Confirms nothing happened, useful when debugging why data didn't reset
            Debug.Log("[ClearSaveData] ClearDataOnStart is unchecked. No data was deleted.");
        }
    }
}