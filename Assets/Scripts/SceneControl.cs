using UnityEngine;
using UnityEngine.UI;

// The different ways a button using SceneControl can navigate, only LoadPreviousScene
// and LoadBySceneName are actually wired up below
public enum sceneDirection
{
    LoadNextScene,
    LoadPreviousScene,
    LoadStartingScene,
    LoadBySceneName
}

// Sits on a button and hooks it up to SceneNavigator based on which direction it's set to
public class SceneControl : MonoBehaviour
{
    // The persistent object that actually handles loading scenes
    private SceneNavigator sceneNavigator; // Renamed
    // The scene name to load, only used when this button loads by name
    private string sceneName;
    // Which kind of navigation this specific button performs, set in the Inspector
    public sceneDirection sceneDirection;
    // The button component this script is attached to
    private Button thisButton;

    // Finds the scene navigator and wires this button's click to the right behavior
    void Start()
    {
        // Looks for the new SceneNavigator component
        sceneNavigator = GameObject.FindWithTag("SceneManager").GetComponent<SceneNavigator>();
        // Grab the button component sitting on this same object
        thisButton = GetComponent<Button>();

        // This button is set to go back to the previous scene
        if (sceneDirection == sceneDirection.LoadPreviousScene)
        {
            // Hook the click straight up to the navigator's back function
            thisButton.onClick.AddListener(sceneNavigator.LoadPreviousScene);
        }
        // This button is set to load a specific named scene
        else if (sceneDirection == sceneDirection.LoadBySceneName)
        {
            // The scene to load is pulled from this button's own tag
            sceneName = thisButton.tag;
            // Hook the click up to load that specific scene by name
            thisButton.onClick.AddListener(delegate {sceneNavigator.LoadSceneByName(sceneName);});
        }
    }
}