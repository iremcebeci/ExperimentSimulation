using System;
using System.Collections.Generic;

namespace ExperimentSimulation.EntityLayer.Concrete
{
    public class CalendarCategory
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string Type { get; set; } = null!;
        public string Label { get; set; } = null!;
        public string Color { get; set; } = "#2e86c1";
        public string TextColor { get; set; } = "#ffffff";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<CalendarEvent> Events { get; set; } = new List<CalendarEvent>();
    }
}
