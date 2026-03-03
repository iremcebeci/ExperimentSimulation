using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperimentSimulation.EntityLayer.Concrete
{
    public class Class
    {
            public int Id { get; set; }
            public string Code { get; set; } = null!;   // örn: "10A", "PHY101"
            public string? Name { get; set; }

            public ICollection<UserClass> UserClasses { get; set; } = new List<UserClass>();
    }
}
