using System;
using System.Collections.Generic;

namespace ExperimentSimulation.EntityLayer.Concrete
{
    public class ContentTask
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;
        public string TaskType { get; set; } = string.Empty;
        public string ExperimentName { get; set; } = string.Empty;
        public string EstimatedDuration { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime Deadline { get; set; }

        public int AssigneeUserId { get; set; }
        public User AssigneeUser { get; set; } = null!;

        public string Priority { get; set; } = "Orta";
        public string Status { get; set; } = "Atandı";
        public int ProgressPercent { get; set; }

        public string Description { get; set; } = string.Empty;
        public string ExpectedOutput { get; set; } = string.Empty;

        public int CreatedByUserId { get; set; }
        public User CreatedByUser { get; set; } = null!;

        public ICollection<ContentTaskComment> Comments { get; set; } = new List<ContentTaskComment>();
        public ICollection<ContentTaskRevisionRequest> RevisionRequests { get; set; } = new List<ContentTaskRevisionRequest>();

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
