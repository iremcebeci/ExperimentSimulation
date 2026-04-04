using System;
using System.Collections.Generic;

namespace ExperimentSimulation.EntityLayer.Concrete
{
    public class TodoItem
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string Title { get; set; } = string.Empty;
        public string Priority { get; set; } = "Orta";
        public DateTime DueDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        public bool IsCompleted { get; set; }
        public DateTime? CompletedAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<TodoSubtask> Subtasks { get; set; } = new List<TodoSubtask>();
    }
}
