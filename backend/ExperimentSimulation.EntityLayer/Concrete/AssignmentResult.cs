using System;
using System.Collections.Generic;

namespace ExperimentSimulation.EntityLayer.Concrete
{
    public class AssignmentResult
    {
        public int Id { get; set; }

        public int AssignmentId { get; set; }
        public Assignment Assignment { get; set; } = null!;

        public int StudentId { get; set; }
        public User Student { get; set; } = null!;

        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public int TotalQuestionCount { get; set; }
        public int Score { get; set; }

        public bool IsCompleted { get; set; } = true;
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

        public ICollection<AssignmentAnswer> Answers { get; set; } = new List<AssignmentAnswer>();
    }
}