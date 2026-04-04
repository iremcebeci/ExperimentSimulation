using System;

namespace ExperimentSimulation.EntityLayer.Concrete
{
    public class TodoSubtask
    {
        public int Id { get; set; }

        public int TodoItemId { get; set; }
        public TodoItem TodoItem { get; set; } = null!;

        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
