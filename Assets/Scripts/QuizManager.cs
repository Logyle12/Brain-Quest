using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public enum RoundResult { Fail, Intermediate, Win }

public class QuizManager : MonoBehaviour
{
    [Header("Quiz Data")]
    [SerializeField] private TextAsset csvData;

    [Header("Progression")]
    public int starsEarned = 0; 
    private float[] starMultipliers = { 0.25f, 0.3125f, 0.4375f, 1.0f };

    public int correctAnswers { get; private set; } = 0;
    public int currentQuestionIndex { get; private set; } = 0;
    public int totalQuestionsThisRound { get { return selectedQuestions.Count; } }

    private string levelSaveKey;
    private List<QuestionData> allQuestions = new List<QuestionData>();
    private List<QuestionData> selectedQuestions = new List<QuestionData>();

    void Start()
    {
        InitializeQuiz();
    }

    public void InitializeQuiz()
    {
        correctAnswers = 0;
        currentQuestionIndex = 0;
        selectedQuestions.Clear();

        // 1. Fetch Subject, Category, and Stage from PlayerPrefs
        string currentSubject = PlayerPrefs.GetString("CurrentSubject", "English");
        string currentCategory = PlayerPrefs.GetString("CurrentCategoryPlaying", "Spelling");
        int currentStage = PlayerPrefs.GetInt("CurrentStagePlaying", 0);
        
        // 2. Generate unique save key
        levelSaveKey = $"{currentSubject}_{currentCategory}_Stage_{currentStage}";
        starsEarned = PlayerPrefs.GetInt(levelSaveKey + "_Stars", 0);

        // 3. Dynamically Load the precise CSV File
        int currentLevel = currentStage + 1; 
        string fileName = $"{currentCategory}_Level_{currentLevel}";
        csvData = Resources.Load<TextAsset>($"QuizData/{currentSubject}/{currentCategory}/{fileName}");

        if (csvData == null)
        {
            Debug.LogError($"[QuizManager] Could not find CSV at: Resources/QuizData/{currentSubject}/{fileName}");
            return;
        }

        allQuestions = CSVReader.ReadCSV(csvData);

        // 4. Apply star multipliers
        int index = Mathf.Clamp(starsEarned, 0, starMultipliers.Length - 1);
        float multiplier = starMultipliers[index];
        int amountToLoad = Mathf.CeilToInt(allQuestions.Count * multiplier);

        // 5. Filter completed questions
        // 5. Filter completed questions (only matters while still working toward 3 stars)
        List<QuestionData> availableQuestions;
        if (starsEarned >= 3)
        {
            // Fully starred: infinite replay, no filtering needed
            availableQuestions = new List<QuestionData>(allQuestions);
        }
        else
        {
            List<int> completedIds = GetCompletedIds(levelSaveKey);
            availableQuestions = allQuestions.Where(q => !completedIds.Contains(q.id)).ToList();
        }

        // 6. SHUFFLE THE AVAILABLE QUESTIONS
        Shuffle(availableQuestions);

        // 7. Select the required amount
        selectedQuestions = availableQuestions.Take(amountToLoad).ToList();

        // 8. SHUFFLE THE ANSWER OPTIONS (so correct isn't always Option A)
        foreach (var q in selectedQuestions)
        {
            ShuffleOptions(q);
        }
    }

    private void ShuffleOptions(QuestionData q)
    {
        if (q.options == null || q.options.Length == 0) return;

        // Memorize what the correct text is before scrambling the array
        string correctAnswerText = q.options[q.correctAnswerIndex];
        
        System.Random rng = new System.Random();
        q.options = q.options.OrderBy(x => rng.Next()).ToArray();
        
        // Find the new index of the correct answer
        q.correctAnswerIndex = Array.IndexOf(q.options, correctAnswerText);
    }

    private void Shuffle<T>(List<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1) 
        { 
            n--; 
            int k = rng.Next(n + 1); 
            T value = list[k]; 
            list[k] = list[n]; 
            list[n] = value; 
        }
    }

    public void ProcessAnswer(bool isCorrect)
    {
        if (isCorrect)
        {
            correctAnswers++;
            if (starsEarned < 3)
            {
                QuestionData q = selectedQuestions[currentQuestionIndex];
                SaveCompletedId(levelSaveKey, q.id);
            }
        }
        currentQuestionIndex++;
    }

    private List<int> GetCompletedIds(string key)
    {
        string ids = PlayerPrefs.GetString(key + "_Completed", "");
        if (string.IsNullOrEmpty(ids)) return new List<int>();
        return ids.Split(',').Select(int.Parse).ToList();
    }

    private void SaveCompletedId(string key, int id)
    {
        List<int> completed = GetCompletedIds(key);
        if (!completed.Contains(id))
        {
            completed.Add(id);
            PlayerPrefs.SetString(key + "_Completed", string.Join(",", completed));
            PlayerPrefs.Save();
        }
    }

    public QuestionData GetCurrentQuestion()
    {
        return (currentQuestionIndex < selectedQuestions.Count) ? selectedQuestions[currentQuestionIndex] : null;
    }

    public void EvaluateRound()
    {
        float percentageCorrect = (float)correctAnswers / selectedQuestions.Count;
        RoundResult finalResult = (percentageCorrect >= 1.0f) ? RoundResult.Win : 
                                  (percentageCorrect > 0.5f) ? RoundResult.Intermediate : RoundResult.Fail;

        if (finalResult == RoundResult.Win && starsEarned < 3) 
        {
            starsEarned++;
            PlayerPrefs.SetInt(levelSaveKey + "_Stars", starsEarned);
            if (starsEarned == 3) PlayerPrefs.DeleteKey(levelSaveKey + "_Completed");
            PlayerPrefs.Save();
        }

        FindObjectOfType<QuizUIManager>().ShowEndScreen(finalResult);
    }
}