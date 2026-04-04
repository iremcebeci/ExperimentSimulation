using System;

namespace ExperimentSimulation.EntityLayer.Concrete
{
    public class UserSessionActivity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime LoginAt { get; set; }
        public DateTime LastSeenAt { get; set; }
        public DateTime? LogoutAt { get; set; }

        public User? User { get; set; }
    }
}
