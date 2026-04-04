using System;

namespace ExperimentSimulation.EntityLayer.Concrete
{
    public class ContentTaskComment
    {
        public int Id { get; set; }

        public int ContentTaskId { get; set; }
        public ContentTask ContentTask { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}