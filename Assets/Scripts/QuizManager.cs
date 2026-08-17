using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

// The three possible outcomes of a quiz round, based on how many questions were answered correctly
public enum RoundResult { Fail, Intermediate, Win }

// Loads a stage's questions, picks which ones this round will use, tracks the player's
// answers as they go, and works out whether the round counts as a win
public class QuizManager : MonoBehaviour
{
    [Header("Quiz Data")]
    // The CSV file currently loaded for this stage, assigned at runtime rather than in the Inspector
    [SerializeField] private TextAsset csvData;

    [Header("Progression")]
    // How many stars have been earned on the stage currently being played
    public int starsEarned = 0;
    // What fraction of the question pool gets loaded, indexed by stars earned so far
    private float[] starMultipliers = { 0.25f, 0.3125f, 0.4375f, 1.0f };

    // How many questions the player has gotten right this round
    public int correctAnswers { get; private set; } = 0;
    // Which question in selectedQuestions is currently being shown
    public int currentQuestionIndex { get; private set; } = 0;
    // How many questions this specific round contains, used by the UI's progress bar
    public int totalQuestionsThisRound { get { return selectedQuestions.Count; } }

    // The PlayerPrefs key prefix for this stage's saved progress, built fresh each time a quiz starts
    private string levelSaveKey;
    // Every question parsed from this stage's CSV, before any filtering
    private List<QuestionData> allQuestions = new List<QuestionData>();
    // The subset of questions actually being asked this round
    private List<QuestionData> selectedQuestions = new List<QuestionData>();

    // Runs once when the scene loads, kicks off the first round
    void Start()
    {
        // Set up the round using whatever subject, category and stage were selected before this scene loaded
        InitializeQuiz();
    }

    // Builds a fresh round, loads the right CSV for the current subject, category and stage,
    // and picks which questions the player will actually see this time
    public void InitializeQuiz()
    {
        // Reset the score for a brand new round
        correctAnswers = 0;
        // Start back at the first question
        currentQuestionIndex = 0;
        // Clear out whatever was selected on a previous round
        selectedQuestions.Clear();

        // Fetch which subject the player is currently on
        string currentSubject = PlayerPrefs.GetString("CurrentSubject", "English");
        // Fetch which category within that subject is being played
        string currentCategory = PlayerPrefs.GetString("CurrentCategoryPlaying", "Spelling");
        // Fetch which stage number within that category is being played
        int currentStage = PlayerPrefs.GetInt("CurrentStagePlaying", 0);

        // Build the key this stage's saved progress lives under
        levelSaveKey = $"{currentSubject}_{currentCategory}_Stage_{currentStage}";
        // Load how many stars have already been earned on this stage
        starsEarned = PlayerPrefs.GetInt(levelSaveKey + "_Stars", 0);

        // Stage numbers are 0 based internally but the CSV files are named starting from 1
        int currentLevel = currentStage + 1;
        // Build the filename for this specific stage's question set
        string fileName = $"{currentCategory}_Level_{currentLevel}";
        // Pull the CSV out of Resources using the subject and category folder structure
        csvData = Resources.Load<TextAsset>($"QuizData/{currentSubject}/{currentCategory}/{fileName}");

        // Bail out if that file doesn't exist, nothing else here can run without it
        if (csvData == null)
        {
            // Log exactly where it looked, makes a missing file easy to track down
            Debug.LogError($"[QuizManager] Could not find CSV at: Resources/QuizData/{currentSubject}/{fileName}");
            return;
        }

        // Parse the CSV into a full list of questions for this stage
        allQuestions = CSVReader.ReadCSV(csvData);

        // Clamp stars earned so it can't index past the end of starMultipliers
        int index = Mathf.Clamp(starsEarned, 0, starMultipliers.Length - 1);
        // The fraction of the question pool this round should include
        float multiplier = starMultipliers[index];
        // Round up so the player always gets at least the intended fraction, never less
        int amountToLoad = Mathf.CeilToInt(allQuestions.Count * multiplier);

        // Questions available to pick from before shuffling and trimming down
        List<QuestionData> availableQuestions;
        // Once fully starred, every question is fair game again since there's nothing left to unlock
        if (starsEarned >= 3)
        {
            // Fully starred, so just copy the whole pool with no filtering
            availableQuestions = new List<QuestionData>(allQuestions);
        }
        else
        {
            // Otherwise figure out which questions have already been answered correctly on this stage
            List<int> completedIds = GetCompletedIds(levelSaveKey);
            // Only offer questions the player hasn't already gotten right
            availableQuestions = allQuestions.Where(q => !completedIds.Contains(q.id)).ToList();
        }

        // Randomize the order so the same questions don't always come up first
        Shuffle(availableQuestions);

        // Trim down to just the amount this round should actually contain
        selectedQuestions = availableQuestions.Take(amountToLoad).ToList();

        // Scramble each question's answer options so the correct one isn't always in the same spot
        foreach (var q in selectedQuestions)
        {
            ShuffleOptions(q);
        }
    }

    // Randomizes one question's answer options while keeping track of where the correct answer ends up
    private void ShuffleOptions(QuestionData q)
    {
        // Nothing to shuffle if this question has no options set up
        if (q.options == null || q.options.Length == 0) return;

        // Memorize what the correct text is before scrambling the array
        string correctAnswerText = q.options[q.correctAnswerIndex];

        // Random number generator used just for this shuffle
        System.Random rng = new System.Random();
        // Reorder the options randomly
        q.options = q.options.OrderBy(x => rng.Next()).ToArray();

        // Find the new index of the correct answer
        q.correctAnswerIndex = Array.IndexOf(q.options, correctAnswerText);
    }

    // Shuffles any list in place using a standard Fisher-Yates shuffle
    private void Shuffle<T>(List<T> list)
    {
        // Random number generator used just for this shuffle
        System.Random rng = new System.Random();
        // Start from the end of the list and work backwards
        int n = list.Count;
        while (n > 1)
        {
            // Move to the next unshuffled slot
            n--;
            // Pick a random index from the remaining unshuffled portion
            int k = rng.Next(n + 1);
            // Swap the current slot with the randomly picked one
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    // Records whether the player's answer was correct, updates saved progress, and moves to the next question
    public void ProcessAnswer(bool isCorrect)
    {
        // Only touch score and save data if the answer was actually right
        if (isCorrect)
        {
            // Add one to the running correct count for this round
            correctAnswers++;
            // No need to track completed questions once the stage is already fully starred
            if (starsEarned < 3)
            {
                // The question that was just answered correctly
                QuestionData q = selectedQuestions[currentQuestionIndex];
                // Remember this question as completed so it won't repeat until the stage is fully starred
                SaveCompletedId(levelSaveKey, q.id);
            }
        }
        // Move on to the next question regardless of right or wrong
        currentQuestionIndex++;
    }

    // Reads back the list of question ids already answered correctly on a given stage
    private List<int> GetCompletedIds(string key)
    {
        // Pull the saved comma separated list of ids, or nothing if none exist yet
        string ids = PlayerPrefs.GetString(key + "_Completed", "");
        // Nothing saved yet, so there's nothing completed
        if (string.IsNullOrEmpty(ids)) return new List<int>();
        // Split the string back into individual numeric ids
        return ids.Split(',').Select(int.Parse).ToList();
    }

    // Adds a question id to the saved list of completed questions for a stage, if it isn't already there
    private void SaveCompletedId(string key, int id)
    {
        // Grab whatever's already been saved as completed
        List<int> completed = GetCompletedIds(key);
        // Only add it if it isn't already recorded
        if (!completed.Contains(id))
        {
            // Add this question to the completed list
            completed.Add(id);
            // Save the updated list back as a comma separated string
            PlayerPrefs.SetString(key + "_Completed", string.Join(",", completed));
            PlayerPrefs.Save();
        }
    }

    // Returns whichever question the player should be looking at right now, or null once the round is over
    public QuestionData GetCurrentQuestion()
    {
        // Only return a question while the index still points inside the selected list
        return (currentQuestionIndex < selectedQuestions.Count) ? selectedQuestions[currentQuestionIndex] : null;
    }

    // Works out whether this round counts as a win, tie or loss, updates saved stars if it's a new
    // best, and tells the UI to show the result
    public void EvaluateRound()
    {
        // What fraction of the round's questions were answered correctly
        float percentageCorrect = (float)correctAnswers / selectedQuestions.Count;
        // A perfect round wins, over half counts as an intermediate pass, anything less is a fail
        RoundResult finalResult = (percentageCorrect >= 1.0f) ? RoundResult.Win :
                                  (percentageCorrect > 0.5f) ? RoundResult.Intermediate : RoundResult.Fail;

        // Only award a star if this was a win and there's still room to earn one
        if (finalResult == RoundResult.Win && starsEarned < 3)
        {
            // Add one star for this clear
            starsEarned++;
            // Save the new star count
            PlayerPrefs.SetInt(levelSaveKey + "_Stars", starsEarned);
            // Once fully starred, the completed question tracking is no longer needed
            if (starsEarned == 3) PlayerPrefs.DeleteKey(levelSaveKey + "_Completed");
            PlayerPrefs.Save();
        }

        // Hand the result off to the UI so it can show the end screen
        FindObjectOfType<QuizUIManager>().ShowEndScreen(finalResult);
    }
}