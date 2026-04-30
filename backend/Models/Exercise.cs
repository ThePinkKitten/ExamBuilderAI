using System.ComponentModel.DataAnnotations;

namespace ExamBuilderAI.API.Models;

public class Exercise
{
    public int Id { get; set; }

    public int SectionId { get; set; }
    public ExerciseSection Section { get; set; } = null!;

    public int? CurriculumUnitId { get; set; }
    public CurriculumUnit? CurriculumUnit { get; set; }

    public int GeneratedByUserId { get; set; }
    public User GeneratedByUser { get; set; } = null!;

    [MaxLength(20)]
    public string Difficulty { get; set; } = "medium";

    public int QuestionCount { get; set; }

    /// <summary>
    /// Full JSON content: questions + correctAnswers + explanations.
    /// Stored as nvarchar(max) and mapped to JSON objects in DTOs.
    /// </summary>
    [Required]
    public string Content { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<ExerciseResult> Results { get; set; } = new List<ExerciseResult>();
}
