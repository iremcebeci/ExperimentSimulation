using System;

namespace ExperimentSimulation.EntityLayer.Concrete
{
    public class CalendarEvent
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int CategoryId { get; set; }
        public CalendarCategory Category { get; set; } = null!;

        public string Title { get; set; } = null!;
        public DateTime Date { get; set; }
        public string Start { get; set; } = "09:00";
        public string End { get; set; } = "10:00";
        public string? Location { get; set; }
        public string? RelatedClass { get; set; }
        public string? Desc { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
