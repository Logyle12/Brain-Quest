using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;

// Static utility that parses a CSV TextAsset into a list of QuestionData objects,
// used by QuizManager to load quiz content per subject, category and stage
public static class CSVReader
{
    // Splits a line on commas, but ignores commas that are inside quoted fields
    static string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
    // Splits the file into lines, handling different line ending styles
    static string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";

    // Reads a CSV file and turns each valid row into a QuestionData object
    public static List<QuestionData> ReadCSV(TextAsset csvFile)
    {
        List<QuestionData> questionList = new List<QuestionData>();

        // Bail out early if no file was actually passed in
        if (csvFile == null)
        {
            Debug.LogError("CSV File is null!");
            return questionList;
        }

        // Break the whole file into individual lines
        string[] lines = Regex.Split(csvFile.text, LINE_SPLIT_RE);

        // Start at 1 to skip the header row
        for (int i = 1; i < lines.Length; i++)
        {
            // Skip any blank lines
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            // Split this row into its individual column values
            string[] values = Regex.Split(lines[i], SPLIT_RE);

            // Skip malformed rows that don't have enough columns
            if (values.Length < 8) continue;

            // Build the question from the row's columns
            QuestionData data = new QuestionData
            {
                id = int.Parse(CleanString(values[0])),
                questionType = CleanString(values[1]),
                questionText = CleanString(values[2]),
                options = new string[] 
                {
                    CleanString(values[3]),
                    CleanString(values[4]),
                    CleanString(values[5]),
                    CleanString(values[6])
                }
            };

            // The CSV stores the answer as 1-4, but the array is 0-indexed, so shift it down by one
            if (int.TryParse(CleanString(values[7]), out int answerNum))
            {
                data.correctAnswerIndex = answerNum - 1; 
            }

            // Add the finished question to the list
            questionList.Add(data);
        }

        return questionList;
    }

    // Trims whitespace, strips wrapping quotes, and unescapes doubled quotes from a raw CSV value
    private static string CleanString(string input)
    {
        // Remove leading/trailing whitespace first
        input = input.Trim();
        // If the whole value is wrapped in quotes, strip them off
        if (input.StartsWith("\"") && input.EndsWith("\""))
        {
            input = input.Substring(1, input.Length - 2);
        }
        // CSV escapes a literal quote as two quotes in a row, so collapse those back down
        return input.Replace("\"\"", "\"");
    }
}