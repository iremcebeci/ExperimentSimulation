using System;

namespace ExperimentSimulation.EntityLayer.Concrete
{
    public class Experiment
    {
        public int Id { get; set; }

        public string GradeLevel { get; set; } = null!;   // 5. Sınıf, 9. Sınıf
        public string LessonName { get; set; } = null!;   // Fen, Fizik, Kimya, Biyoloji
        public string UnitName { get; set; } = null!;     // Kuvvet ve Hareket
        public string ExperimentName { get; set; } = null!; // Vektörler

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? SceneName { get; set; }
        public string? ExperimentKey { get; set; }
    }
}