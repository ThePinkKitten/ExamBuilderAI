using System.ComponentModel.DataAnnotations;

namespace ExamBuilderAI.API.Models;

public class Question
{
    public int Id { get; set; }
    
    public int SectionId { get; set; }
    public ExerciseSection Section { get; set; } = null!;
    
    public int? CurriculumUnitId { get; set; }
    public CurriculumUnit? CurriculumUnit { get; set; }

    /// <summary>
    /// Full JSON containing question, options, correct answer, explanation.
    /// </summary>
    [Required]
    public string Content { get; set; } = "{}";

    /// <summary>
    /// True if the question has been assigned to at least one Exercise.
    /// </summary>
    public bool IsAssigned { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ExerciseQuestion> ExerciseQuestions { get; set; } = new List<ExerciseQuestion>();
}
