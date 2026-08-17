# Building Blocks

Building Blocks is an educational quiz game built in Unity for Key Stage 1 and Key Stage 2 students. It helps children practice Mathematics, English, and Science through interactive quizzes that increase in difficulty from Year 1 to Year 6. The project is designed to be easy to expand, with all quiz content stored in external CSV files rather than hardcoded into the application.

## Features

### Core Functionality

- **Multiple Subjects:** Practice Mathematics, English, and Science from a single application.
- **Six Difficulty Levels:** Questions are organised into Levels 1 to 6, matching the UK primary school curriculum from Year 1 through Year 6.
- **Data-Driven Content:** Quiz questions are loaded from external CSV files, making it easy to add or update content without changing the code.
- **Progress Tracking:** Completion progress and star ratings are saved locally so students can continue where they left off.
- **Navigation History:** A stack-based navigation system allows the Back button to return users to the previous screen instead of always returning to the main menu.

### Accessibility

- **Adjustable Text Size:** Increase or decrease font size to improve readability.
- **Multiple Font Options:** Choose from different fonts to suit individual reading preferences.
- **Responsive Interface:** Menus and quiz screens automatically adapt to different screen sizes and resolutions.

## Installation

To run the project locally:

1. Clone the repository.

   ```bash
   git clone git@github.com:Logyle12/BuildingBlocks2.git
   ```

2. Open the project in Unity.

3. Press **Play** in the Unity Editor.

## Live Preview

[Click here to view the live preview.](https://logyle12.github.io/Brain-Quest/)

## Usage

- Select a subject and difficulty level.
- Answer each question by choosing one of the available options.
- Complete quizzes to earn stars and track your progress.
- Add or edit quiz content by updating the CSV files in the data folder.

## Data Format

Quiz data is loaded from CSV files. Each file should include the following columns:

| Column | Description |
| --- | --- |
| **#** | Unique question ID |
| **Question Type** | Subject or category |
| **Question** | The question text |
| **Option A** | First answer choice |
| **Option B** | Second answer choice |
| **Option C** | Third answer choice |
| **Option D** | Fourth answer choice |
| **Answer** | Correct answer (1 to 4) |

## Tech Stack

- Unity
- C#
- TextMeshPro
- LeanTween

## File Structure

```text
|-- Assets
|   |-- Data
|   |   |-- CSV quiz files
|   |
|   |-- Scripts
|       |-- Audio
|       |-- Data
|       |-- Levels
|       |-- Navigation
|       |-- Quiz
|       |-- UI
|
|-- README.md
```

## License

This project is licensed under the MIT License.
