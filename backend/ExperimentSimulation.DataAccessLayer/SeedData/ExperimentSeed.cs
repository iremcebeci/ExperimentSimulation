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
                new Experiment { GradeLevel = "5", LessonName = "Fen", UnitName = "Kuvveti Tanıyalım", ExperimentName = "Kuvvetin Ölçülmesi" },
                new Experiment { GradeLevel = "5", LessonName = "Fen", UnitName = "Kuvveti Tanıyalım", ExperimentName = "Sürtünme Kuvveti" },
                new Experiment { GradeLevel = "5", LessonName = "Fen", UnitName = "Işığın Dünyası", ExperimentName = "Işığın Yayılması" },
                new Experiment { GradeLevel = "5", LessonName = "Fen", UnitName = "Işığın Dünyası", ExperimentName = "Gölgenin Oluşumu" },

                new Experiment { GradeLevel = "6", LessonName = "Fen", UnitName = "ELEKTRİĞİN İLETİMİ VE DİRENÇ", ExperimentName = "Elektriğin İletimi" },
                new Experiment { GradeLevel = "6", LessonName = "Fen", UnitName = "ELEKTRİĞİN İLETİMİ VE DİRENÇ", ExperimentName = "Direnç" },
                new Experiment { GradeLevel = "6", LessonName = "Fen", UnitName = "IŞIĞIN YANSIMASI", ExperimentName = "Işığın Yansıması" },
                new Experiment { GradeLevel = "6", LessonName = "Fen", UnitName = "IŞIĞIN YANSIMASI", ExperimentName = "Aynalar" },

                new Experiment { GradeLevel = "7", LessonName = "Fen", UnitName = "Hücre ve Bölünmeler", ExperimentName = "Mitoz" },
                new Experiment { GradeLevel = "7", LessonName = "Fen", UnitName = "Hücre ve Bölünmeler", ExperimentName = "Mayoz" },
                new Experiment { GradeLevel = "7", LessonName = "Fen", UnitName = "Saf Madde ve Karışımlar", ExperimentName = "Saf Maddeler" },
                new Experiment { GradeLevel = "7", LessonName = "Fen", UnitName = "Saf Madde ve Karışımlar", ExperimentName = "Karışımlar" },

                new Experiment { GradeLevel = "8", LessonName = "Fen", UnitName = "Madde ve Endüstri", ExperimentName = "Periyodik Sistem" },
                new Experiment { GradeLevel = "8", LessonName = "Fen", UnitName = "Madde ve Endüstri", ExperimentName = "Asitler ve Bazlar" },
                new Experiment { GradeLevel = "8", LessonName = "Fen", UnitName = "Elektrik", ExperimentName = "Yüklü Cisimler" },
                new Experiment { GradeLevel = "8", LessonName = "Fen", UnitName = "Elektrik", ExperimentName = "Elektrik Enerjisinin Dönüşümü" },

                new Experiment { GradeLevel = "9", LessonName = "Fizik", UnitName = "KUVVET ve HAREKET", ExperimentName = "Vektörler" },
                new Experiment { GradeLevel = "9", LessonName = "Fizik", UnitName = "KUVVET ve HAREKET", ExperimentName = "Doğadaki Temel Kuvvetler" },
                new Experiment { GradeLevel = "9", LessonName = "Fizik", UnitName = "AKIŞKANLAR", ExperimentName = "Sıvılarda Basınç" },
                new Experiment { GradeLevel = "9", LessonName = "Fizik", UnitName = "AKIŞKANLAR", ExperimentName = "Açık Hava Basıncı" },

                new Experiment { GradeLevel = "9", LessonName = "Kimya", UnitName = "Etkileşimler", ExperimentName = "Metalik Bağ" },
                new Experiment { GradeLevel = "9", LessonName = "Kimya", UnitName = "Etkileşimler", ExperimentName = "İyonik Bağ" },
                new Experiment { GradeLevel = "9", LessonName = "Kimya", UnitName = "Atomdan Periyodik Tabloya", ExperimentName = "Atom Teorileri ve Atomun Yapısı" },
                new Experiment { GradeLevel = "9", LessonName = "Kimya", UnitName = "Atomdan Periyodik Tabloya", ExperimentName = "Periyodik Özellikler" },

                new Experiment { GradeLevel = "9", LessonName = "Biyoloji", UnitName = "Yaşam Bilimi Biyoloji", ExperimentName = "Canlıların Ortak Özellikleri" },
                new Experiment { GradeLevel = "9", LessonName = "Biyoloji", UnitName = "Yaşam Bilimi Biyoloji", ExperimentName = "Bilimsel Gözlem ve Sınıflandırma" },
                new Experiment { GradeLevel = "9", LessonName = "Biyoloji", UnitName = "Hücre", ExperimentName = "Hücre Zarından Madde Geçişi" },
                new Experiment { GradeLevel = "9", LessonName = "Biyoloji", UnitName = "Hücre", ExperimentName = "Mikroskopta Hücre İncelemesi" },

                new Experiment { GradeLevel = "10", LessonName = "Fizik", UnitName = "Elektrik ve Manyetizma", ExperimentName = "Elektrik Akımı ve Direnç" },
                new Experiment { GradeLevel = "10", LessonName = "Fizik", UnitName = "Elektrik ve Manyetizma", ExperimentName = "Ohm Kanunu" },
                new Experiment { GradeLevel = "10", LessonName = "Fizik", UnitName = "Dalgalar", ExperimentName = "Yay Dalgaları" },
                new Experiment { GradeLevel = "10", LessonName = "Fizik", UnitName = "Dalgalar", ExperimentName = "Su Dalgalarında Yansıma" },

                new Experiment { GradeLevel = "10", LessonName = "Kimya", UnitName = "Kimyanın Temel Kanunları ve Kimyasal Hesaplamalar", ExperimentName = "Mol Kavramı" },
                new Experiment { GradeLevel = "10", LessonName = "Kimya", UnitName = "Kimyanın Temel Kanunları ve Kimyasal Hesaplamalar", ExperimentName = "Kimyasal Tepkime Hesaplamaları" },
                new Experiment { GradeLevel = "10", LessonName = "Kimya", UnitName = "Karışımlar", ExperimentName = "Homojen ve Heterojen Karışımlar" },
                new Experiment { GradeLevel = "10", LessonName = "Kimya", UnitName = "Karışımlar", ExperimentName = "Ayırma Yöntemleri" },

                new Experiment { GradeLevel = "10", LessonName = "Biyoloji", UnitName = "Kalıtımın Genel İlkeleri", ExperimentName = "Mendel Deneyleri" },
                new Experiment { GradeLevel = "10", LessonName = "Biyoloji", UnitName = "Kalıtımın Genel İlkeleri", ExperimentName = "Baskın ve Çekinik Özellikler" },
                new Experiment { GradeLevel = "10", LessonName = "Biyoloji", UnitName = "Ekosistem Ekolojisi ve Güncel Çevre Sorunları", ExperimentName = "Besin Zinciri" },
                new Experiment { GradeLevel = "10", LessonName = "Biyoloji", UnitName = "Ekosistem Ekolojisi ve Güncel Çevre Sorunları", ExperimentName = "Madde Döngüleri" },

                new Experiment { GradeLevel = "11", LessonName = "Fizik", UnitName = "Kuvvet ve Hareket", ExperimentName = "Tork" },
                new Experiment { GradeLevel = "11", LessonName = "Fizik", UnitName = "Kuvvet ve Hareket", ExperimentName = "Denge Şartları" },
                new Experiment { GradeLevel = "11", LessonName = "Fizik", UnitName = "Basit Makineler", ExperimentName = "Kaldıraç Sistemleri" },
                new Experiment { GradeLevel = "11", LessonName = "Fizik", UnitName = "Basit Makineler", ExperimentName = "Makaralar ve Verim" },

                new Experiment { GradeLevel = "11", LessonName = "Kimya", UnitName = "Modern Atom Teorisi", ExperimentName = "Atom Orbitalleri" },
                new Experiment { GradeLevel = "11", LessonName = "Kimya", UnitName = "Modern Atom Teorisi", ExperimentName = "Elektron Dizilimi" },
                new Experiment { GradeLevel = "11", LessonName = "Kimya", UnitName = "Gazlar", ExperimentName = "Gaz Basıncı" },
                new Experiment { GradeLevel = "11", LessonName = "Kimya", UnitName = "Gazlar", ExperimentName = "Boyle - Charles Yasaları" },

                new Experiment { GradeLevel = "11", LessonName = "Biyoloji", UnitName = "Genden Proteine", ExperimentName = "DNA Modeli" },
                new Experiment { GradeLevel = "11", LessonName = "Biyoloji", UnitName = "Genden Proteine", ExperimentName = "Protein Sentezi" },
                new Experiment { GradeLevel = "11", LessonName = "Biyoloji", UnitName = "Canlılarda Enerji Dönüşümleri", ExperimentName = "Fotosentez" },
                new Experiment { GradeLevel = "11", LessonName = "Biyoloji", UnitName = "Canlılarda Enerji Dönüşümleri", ExperimentName = "Hücresel Solunum" },

                new Experiment { GradeLevel = "12", LessonName = "Fizik", UnitName = "Çembersel Hareket", ExperimentName = "Düzgün Çembersel Hareket" },
                new Experiment { GradeLevel = "12", LessonName = "Fizik", UnitName = "Çembersel Hareket", ExperimentName = "Merkezcil Kuvvet" },
                new Experiment { GradeLevel = "12", LessonName = "Fizik", UnitName = "Modern Fizik", ExperimentName = "Fotoelektrik Olay" },
                new Experiment { GradeLevel = "12", LessonName = "Fizik", UnitName = "Modern Fizik", ExperimentName = "Compton Olayı" },

                new Experiment { GradeLevel = "12", LessonName = "Kimya", UnitName = "Kimya ve Elektrik", ExperimentName = "Galvanik Hücre" },
                new Experiment { GradeLevel = "12", LessonName = "Kimya", UnitName = "Kimya ve Elektrik", ExperimentName = "Elektroliz" },
                new Experiment { GradeLevel = "12", LessonName = "Kimya", UnitName = "Karbon Kimyasına Giriş", ExperimentName = "Hidrokarbonlar" },
                new Experiment { GradeLevel = "12", LessonName = "Kimya", UnitName = "Karbon Kimyasına Giriş", ExperimentName = "Fonksiyonel Gruplar" },

                new Experiment { GradeLevel = "12", LessonName = "Biyoloji", UnitName = "Genden Proteine", ExperimentName = "Nükleik Asitler" },
                new Experiment { GradeLevel = "12", LessonName = "Biyoloji", UnitName = "Genden Proteine", ExperimentName = "Replikasyon ve Protein Sentezi" },
                new Experiment { GradeLevel = "12", LessonName = "Biyoloji", UnitName = "Canlılarda Enerji Dönüşümleri", ExperimentName = "Fotosentez" },
                new Experiment { GradeLevel = "12", LessonName = "Biyoloji", UnitName = "Canlılarda Enerji Dönüşümleri", ExperimentName = "Kemosentez" }
            };
        }
    }
}