using ExperimentSimulation.BusinessLayer.Abstract;
using ExperimentSimulation.EntityLayer.Concrete;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExperimentSimulation.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        public IActionResult RoleList()
        {
            var values = _roleService.TGetList();
            return Ok(values);

        }
        [HttpPost]
        public IActionResult AddRole(Role role)
        {
            _roleService.TInsert(role);
            return Ok();
        }
        [HttpDelete]
        public IActionResult DeleteRole(int id)
        {
            var values = _roleService.TGetByID(id);
            _roleService.TDelete(values);
            return Ok();
        }
        [HttpPut]
        public IActionResult UpdateRole(Role role)
        {
            _roleService.TUpdate(role);
            return Ok();
        }
        [HttpGet("{id}")]
        public IActionResult GetRole(int id)
        {
            var values = _roleService.TGetByID(id);
            return Ok(values);
        }
    }
}
