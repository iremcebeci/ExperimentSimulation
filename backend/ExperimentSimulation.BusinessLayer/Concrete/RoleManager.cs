using ExperimentSimulation.BusinessLayer.Abstract;
using ExperimentSimulation.DataAccessLayer.Abstract;
using ExperimentSimulation.EntityLayer.Concrete;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperimentSimulation.BusinessLayer.Concrete
{
    public class RoleManager : IRoleService
    {
        private readonly IRoleDal _roleDal;
        public RoleManager(IRoleDal roleDal)
        {
            _roleDal = roleDal;
        }
        public void TDelete(Role t)
        {
            _roleDal.Delete(t);
        }

        public Role TGetByID(int id)
        {
            return _roleDal.GetByID(id);
        }

        public List<Role> TGetList()
        {
            return _roleDal.GetList();
        }

        public void TInsert(Role t)
        {
            _roleDal.Insert(t);
        }

        public void TUpdate(Role t)
        {
            _roleDal.Update(t);
        }
    }
}
