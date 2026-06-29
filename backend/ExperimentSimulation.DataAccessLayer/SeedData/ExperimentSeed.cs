using System.Collections.Generic;
using ExperimentSimulation.EntityLayer.Concrete;

namespace ExperimentSimulation.DataAccessLayer.SeedData
{
    public static class ExperimentSeed
    {
        public static List<Experiment> GetSeeds()
        {
            return new List<Experiment>
            {
                new Experiment
                {
                    GradeLevel = "5",
                    LessonName = "Fen",
                    UnitName = "Gökyüzündeki Komşularımız ve Biz",
                    ExperimentName = "Güneş, Dünya ve Ay Etkileşimleri",
                    SceneName = "SolarSystemScene",
                    ExperimentKey = "sun_earth_moon",
                    IsActive = true
                },

                new Experiment
                {
                    GradeLevel = "5",
                    LessonName = "Fen",
                    UnitName = "Gökyüzündeki Komşularımız ve Biz",
                    ExperimentName = "Güneş Tutulması Simülasyonu",
                    SceneName = "SolarEclipseScene",
                    ExperimentKey = "solar_eclipse",
                    IsActive = false
                },

                new Experiment
                {
                    GradeLevel = "5",
                    LessonName = "Fen",
                    UnitName = "Gökyüzündeki Komşularımız ve Biz",
                    ExperimentName = "Ay Tutulması Simülasyonu",
                    SceneName = "LunarEclipseScene",
                    ExperimentKey = "lunar_eclipse",
                    IsActive = false
                },

                new Experiment
                {
                    GradeLevel = "5",
                    LessonName = "Fen",
                    UnitName = "Gökyüzündeki Komşularımız ve Biz",
                    ExperimentName = "Mevsimlerin Oluşumu",
                    SceneName = "SeasonsScene",
                    ExperimentKey = "seasons",
                    IsActive = false
                },

                new Experiment
                {
                    GradeLevel = "5",
                    LessonName = "Fen",
                    UnitName = "Gökyüzündeki Komşularımız ve Biz",
                    ExperimentName = "Ekinoks Simülasyonu",
                    SceneName = "EquinoxScene",
                    ExperimentKey = "equinox",
                    IsActive = false
                },

                new Experiment
                {
                    GradeLevel = "5",
                    LessonName = "Fen",
                    UnitName = "Gökyüzündeki Komşularımız ve Biz",
                    ExperimentName = "Solstis Simülasyonu",
                    SceneName = "SolsticeScene",
                    ExperimentKey = "solstice",
                    IsActive = false
                }
            };
        }
    }
}