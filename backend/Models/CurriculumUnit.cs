using System.ComponentModel.DataAnnotations;

namespace ExamBuilderAI.API.Models;

public class CurriculumUnit
{
    public int Id { get; set; }

    public int Grade { get; set; } = 8;

    public int UnitNumber { get; set; }

    [Required, MaxLength(200)]
    public string UnitTitle { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of grammar points covered in this unit.
    /// E.g.: ["Past Continuous with when/while", "Conditional Type 1"]
    /// </summary>
    public string GrammarPoints { get; set; } = "[]";

    /// <summary>
    /// JSON array of key vocabulary words for this unit.
    /// E.g.: ["earthquake", "flood", "drought", "tornado"]
    /// </summary>
    public string Vocabulary { get; set; } = "[]";

    /// <summary>
    /// JSON array of topics/themes for this unit.
    /// E.g.: ["natural disasters", "environmental protection"]
    /// </summary>
    public string Topics { get; set; } = "[]";

    // Navigation
    public ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
}
