using ExperimentSimulation.DataAccessLayer.Concrete;
using Microsoft.AspNetCore.Mvc;

namespace ExperimentSimulation.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExperimentController : ControllerBase
    {
        private readonly Context _context;

        public ExperimentController(Context context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var data = _context.Experiments
                .Where(x => x.IsActive)
                .OrderBy(x => x.GradeLevel)
                .ThenBy(x => x.LessonName)
                .ThenBy(x => x.UnitName)
                .ThenBy(x => x.ExperimentName)
                .ToList();

            return Ok(data);
        }

        [HttpGet("by-lesson")]
        public IActionResult GetByLesson([FromQuery] string lessonName)
        {
            var data = _context.Experiments
                .Where(x => x.IsActive && x.LessonName == lessonName)
                .OrderBy(x => x.UnitName)
                .ThenBy(x => x.ExperimentName)
                .ToList();

            return Ok(data);
        }

        [HttpGet("by-grade-lesson")]
        public IActionResult GetByGradeAndLesson([FromQuery] string gradeLevel, [FromQuery] string lessonName)
        {
            var data = _context.Experiments
                .Where(x => x.IsActive &&
                            x.GradeLevel == gradeLevel &&
                            x.LessonName == lessonName)
                .OrderBy(x => x.UnitName)
                .ThenBy(x => x.ExperimentName)
                .ToList();

            return Ok(data);
        }
    }
}