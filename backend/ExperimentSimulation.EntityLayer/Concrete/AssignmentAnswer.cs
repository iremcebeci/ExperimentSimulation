using System;

namespace ExperimentSimulation.EntityLayer.Concrete
{
    public class AssignmentAnswer
    {
        public int Id { get; set; }

        public int AssignmentResultId { get; set; }
        public AssignmentResult AssignmentResult { get; set; } = null!;

        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;

        public int StudentId { get; set; }
        public User Student { get; set; } = null!;

        public string QuestionText { get; set; } = string.Empty;
        public string StudentAnswer { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
