using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace ExamBuilderAI.API.DTOs.Exercise;

public class GenerateExerciseRequest
{
    [Required]
    public string SectionCode { get; set; } = string.Empty;

    /// <summary>
    /// Optional curriculum unit ID. If provided, AI will scope content to that unit.
    /// </summary>
    public int? CurriculumUnitId { get; set; }

    [Range(1, 20)]
    public int QuestionCount { get; set; } = 5;
}

public class RetakeMistakesRequest
{
    /// <summary>
    /// Optional section code to scope mistakes to a specific section.
    /// If null, pulls mistakes globally.
    /// </summary>
    public string? SectionCode { get; set; }

    /// <summary>
    /// Number of mistakes to retake (default 10).
    /// </summary>
    [Range(1, 20)]
    public int? QuestionCount { get; set; } = 10;
}

public class SubmitExerciseRequest
{
    /// <summary>
    /// User answers as a dictionary. Key = question ID (string), Value = answer.
    /// For MCQ: value is the selected option index (int).
    /// For fill-in: value is the text answer (string).
    /// For writing: value is the full paragraph text (string).
    /// </summary>
    [Required]
    public Dictionary<string, JsonElement> UserAnswers { get; set; } = new();

    public int TimeTakenSeconds { get; set; }
}

public class ExerciseResponse
{
    public int Id { get; set; }
    public string SectionCode { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public string? UnitTitle { get; set; }
    public int QuestionCount { get; set; }

    /// <summary>
    /// Questions only — correctAnswer and explanation are STRIPPED.
    /// </summary>
    public JsonElement Questions { get; set; }

    public DateTime CreatedAt { get; set; }
    public bool HasBeenSubmitted { get; set; }
}

public class ExerciseResultResponse
{
    public int ExerciseId { get; set; }
    public string SectionCode { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public int CorrectCount { get; set; }
    public int TotalQuestions { get; set; }
    public double ScorePercent { get; set; }
    public int TimeTakenSeconds { get; set; }

    /// <summary>
    /// Full content WITH answers and explanations — only returned after submit.
    /// </summary>
    public JsonElement FullContent { get; set; }

    /// <summary>
    /// The user's submitted answers for review.
    /// </summary>
    public JsonElement UserAnswers { get; set; }

    /// <summary>
    /// AI feedback for writing exercises only.
    /// </summary>
    public string? AiFeedback { get; set; }

    public DateTime CompletedAt { get; set; }
}

public class ExerciseHistoryItem
{
    public int ExerciseId { get; set; }
    public string SectionCode { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public string? UnitTitle { get; set; }
    public double? ScorePercent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class SectionInfo
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int TotalExercises { get; set; }
    public double? AverageScore { get; set; }
}

public class CurriculumUnitInfo
{
    public int Id { get; set; }
    public int UnitNumber { get; set; }
    public string UnitTitle { get; set; } = string.Empty;
}
