using ExperimentSimulation.DataAccessLayer.Abstract;
using ExperimentSimulation.DataAccessLayer.Concrete;
using ExperimentSimulation.DataAccessLayer.Repositories;
using ExperimentSimulation.EntityLayer.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperimentSimulation.DataAccessLayer.EntityFramework
{
    public class EfUserDal : GenericRepository<User>, IUserDal
    {
        public EfUserDal(Context context) : base(context)
        {
        }
    }
}
