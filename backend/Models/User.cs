using System.ComponentModel.DataAnnotations;

namespace ExamBuilderAI.API.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public ICollection<Exercise> GeneratedExercises { get; set; } = new List<Exercise>();
    public ICollection<ExerciseResult> ExerciseResults { get; set; } = new List<ExerciseResult>();
}
