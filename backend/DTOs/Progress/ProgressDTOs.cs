namespace ExamBuilderAI.API.DTOs.Progress;

public class ProgressOverview
{
    public int TotalExercisesDone { get; set; }
    public double OverallAverageScore { get; set; }
    public int TotalQuestionsAnswered { get; set; }
    public int TotalCorrectAnswers { get; set; }
    public List<SectionProgress> SectionStats { get; set; } = new();
}

public class SectionProgress
{
    public string SectionCode { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public int ExercisesDone { get; set; }
    public double AverageScore { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
}
