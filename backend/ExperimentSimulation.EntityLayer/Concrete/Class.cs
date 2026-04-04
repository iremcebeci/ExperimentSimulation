using System;
using System.Collections.Generic;

namespace ExperimentSimulation.EntityLayer.Concrete
{
    public class Class
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string? Name { get; set; }

        public ICollection<UserClass> UserClasses { get; set; } = new List<UserClass>();
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();

        public string? LessonName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public string? GradeLevel { get; set; }
    }
}