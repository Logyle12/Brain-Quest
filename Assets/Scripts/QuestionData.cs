// Holds a single quiz question and everything needed to display and grade it,
// gets built by CSVReader and consumed by QuizManager and QuizUIManager
[System.Serializable]
public class QuestionData
{
    // Unique number for this question, used to track which ones the player has already completed
    public int id;
    // What kind of question this is, read from the CSV but not currently branched on
    public string questionType;
    // The actual question shown to the player
    public string questionText;
    // The four possible answers shown as buttons
    public string[] options = new string[4];
    // Which index in options is the correct answer
    public int correctAnswerIndex;
    // Not currently used for selection weighting, but reserved for future use
    public float weight = 1.0f;
}