using System;
using System.Collections.Generic;

namespace ExperimentSimulation.EntityLayer.Concrete
{
    public class Assignment
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public DateTime StartDate { get; set; }
        public int DurationDays { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int ClassId { get; set; }
        public Class Class { get; set; } = null!;
        public int ExperimentId { get; set; }
        public Experiment Experiment { get; set; } = null!;
        public ICollection<AssignmentResult> Results { get; set; } = new List<AssignmentResult>();
    }
}