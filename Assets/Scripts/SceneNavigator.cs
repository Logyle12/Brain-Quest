using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

// Persistent scene loader that keeps a history of visited scenes, so any button in the
// game can load a scene by name or step back to whatever came before it
public class SceneNavigator : MonoBehaviour
{
    // How long to wait before a scene actually loads, gives transitions or sounds time to play
    public float delayDuration = 1f;
    // Every scene visited so far, most recent on top
    private Stack<string> sceneHistory = new Stack<string>();

    // Runs as soon as this object wakes up, makes sure only one navigator ever exists
    private void Awake()
    {
        // More than one navigator already exists, so this one is a duplicate from a new scene
        if (GameObject.FindGameObjectsWithTag("SceneManager").Length > 1)
        {
            // Get rid of the duplicate and leave the original in charge
            Destroy(gameObject);
            return;
        }
        // This is the only navigator, so keep it alive across scene changes
        DontDestroyOnLoad(gameObject);
    }

    // Records the very first scene as the start of the history
    private void Start()
    {
        // Push whatever scene is currently active onto the history stack
        sceneHistory.Push(SceneManager.GetActiveScene().name);
    }

    // Called by any button that wants to load a specific scene by name
    public void LoadSceneByName(string sceneName)
    {
        // Load it after the usual delay, and remember it in the history
        StartCoroutine(LoadRoutine(sceneName, true));
    }

    // Called by a back button to return to whatever scene was visited before this one
    public void LoadPreviousScene()
    {
        // Only step back if there's actually a scene before the current one
        if (sceneHistory.Count > 1)
        {
            // Drop the current scene off the top of the history
            sceneHistory.Pop();
            // The scene now on top is the one to go back to
            string previousScene = sceneHistory.Peek();
            // Load it after the usual delay, without pushing it again since it's already in the history
            StartCoroutine(LoadRoutine(previousScene, false));
        }
        else
        {
            // Nothing left to go back to
            Debug.LogWarning("You are at the very beginning, cannot go back!");
        }
    }

    // Waits out the delay, then actually loads the scene, updating history only when moving forward
    private IEnumerator LoadRoutine(string sceneName, bool isMovingForward)
    {
        // Give whatever transition or effect is playing time to finish
        yield return new WaitForSeconds(delayDuration);

        // Only add to the history when this is a fresh scene being navigated to
        if (isMovingForward)
        {
            // Record this scene as the newest entry in the history
            sceneHistory.Push(sceneName);
        }

        // Actually switch over to the new scene
        SceneManager.LoadScene(sceneName);
    }
}