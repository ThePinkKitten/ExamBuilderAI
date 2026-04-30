using System.ComponentModel.DataAnnotations;

namespace ExamBuilderAI.API.Models;

public class ExerciseResult
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    /// <summary>
    /// JSON object containing the user's answers.
    /// E.g.: {"1": 2, "2": 0, "3": 3} for MCQ or {"1": "creative", "2": "pollution"} for fill-in.
    /// </summary>
    public string UserAnswers { get; set; } = "{}";

    public int CorrectCount { get; set; }
    public int TotalQuestions { get; set; }
    public double ScorePercent { get; set; }
    public int TimeTakenSeconds { get; set; }

    /// <summary>
    /// Optional AI feedback for writing exercises.
    /// </summary>
    public string? AiFeedback { get; set; }

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
