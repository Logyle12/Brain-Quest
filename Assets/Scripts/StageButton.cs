using UnityEngine;
using UnityEngine.SceneManagement;

// Sits on a stage tile button, saves which stage was picked and loads the quiz scene
public class StageButton : MonoBehaviour
{
    // Which category this stage belongs to, set by LevelManager when the level list is built
    private string myCategory;
    // Which stage number within that category this button represents
    private int myStageIndex;
    // The name of the scene to load when this stage is played
    public string quizSceneName = "QuizScene";

    // Called by LevelManager to tell this button which category and stage it represents
    public void SetupButtonData(string category, int index)
    {
        // Store the category this button belongs to
        myCategory = category;
        // Store the stage number this button represents
        myStageIndex = index;
    }

    // Called when the player taps this stage, saves what was picked and loads the quiz
    public void OnStageClicked()
    {
        // Save which category is being played
        PlayerPrefs.SetString("CurrentCategoryPlaying", myCategory);
        // Save which stage number within that category is being played
        PlayerPrefs.SetInt("CurrentStagePlaying", myStageIndex);

        // Remember exactly which scene the player is leaving, so the quiz knows where to return to
        PlayerPrefs.SetString("ReturnSceneName", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();

        // Find the persistent navigator that actually handles scene loading
        SceneNavigator navigator = GameObject.FindWithTag("SceneManager").GetComponent<SceneNavigator>();
        // Load the quiz scene
        navigator.LoadSceneByName(quizSceneName);
    }
}