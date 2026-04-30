using System.ComponentModel.DataAnnotations;

namespace ExamBuilderAI.API.Models;

public class ExerciseSection
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Icon { get; set; } = "quiz";

    public int DisplayOrder { get; set; }

    // Navigation
    public ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
}
