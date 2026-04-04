using System;

namespace ExperimentSimulation.EntityLayer.Concrete
{
    public class ContentTaskRevisionRequest
    {
        public int Id { get; set; }

        public int ContentTaskId { get; set; }
        public ContentTask ContentTask { get; set; } = null!;

        public int RequestedByUserId { get; set; }
        public User RequestedByUser { get; set; } = null!;

        public string RevisionType { get; set; } = string.Empty;
        public string Priority { get; set; } = "Orta";
        public DateTime? NewDeadline { get; set; }
        public string Note { get; set; } = string.Empty;

        public bool IsResolved { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}