namespace ExamBuilderAI.API.Models;

public class ExerciseQuestion
{
    public int ExerciseId { get; set; }
    public Exercise Exercise { get; set; } = null!;

    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    public int OrderIndex { get; set; }
}
