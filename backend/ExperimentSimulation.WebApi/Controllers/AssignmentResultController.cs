using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using ExperimentSimulation.DataAccessLayer.Concrete;
using ExperimentSimulation.EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExperimentSimulation.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AssignmentResultController : ControllerBase
    {
        private readonly Context _context;

        private static readonly JsonSerializerOptions PascalCaseJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null
        };

        public AssignmentResultController(Context context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult Submit([FromBody] SubmitAssignmentResultDto dto)
        {
            int studentId = GetCurrentUserId();

            if (studentId <= 0)
            {
                return Unauthorized(new
                {
                    message = "Kullanıcı kimliği bulunamadı."
                });
            }

            if (dto == null)
            {
                return BadRequest(new
                {
                    message = "Gönderilen veri boş."
                });
            }

            if (dto.AssignmentId <= 0)
            {
                return BadRequest(new
                {
                    message = "AssignmentId geçersiz."
                });
            }

            var assignment = _context.Assignments.FirstOrDefault(x => x.Id == dto.AssignmentId);

            if (assignment == null)
            {
                return NotFound(new
                {
                    message = "Ödev bulunamadı."
                });
            }

            int totalQuestionCount = dto.TotalQuestionCount;

            if (totalQuestionCount <= 0)
            {
                totalQuestionCount = dto.CorrectCount + dto.WrongCount;
            }

            int score = 0;

            if (totalQuestionCount > 0)
            {
                score = (int)Math.Round(dto.CorrectCount / (double)totalQuestionCount * 100);
            }

            var existingResult = _context.AssignmentResults.FirstOrDefault(x =>
                x.AssignmentId == dto.AssignmentId &&
                x.StudentId == studentId
            );

            if (existingResult == null)
            {
                existingResult = new AssignmentResult
                {
                    AssignmentId = dto.AssignmentId,
                    StudentId = studentId,
                    CorrectCount = dto.CorrectCount,
                    WrongCount = dto.WrongCount,
                    TotalQuestionCount = totalQuestionCount,
                    Score = score,
                    IsCompleted = true,
                    CompletedAt = DateTime.UtcNow
                };

                _context.AssignmentResults.Add(existingResult);
            }
            else
            {
                existingResult.CorrectCount = dto.CorrectCount;
                existingResult.WrongCount = dto.WrongCount;
                existingResult.TotalQuestionCount = totalQuestionCount;
                existingResult.Score = score;
                existingResult.IsCompleted = true;
                existingResult.CompletedAt = DateTime.UtcNow;
            }

            _context.SaveChanges();

            if (dto.Answers != null)
            {
                var oldAnswers = _context.AssignmentAnswers
                    .Where(x => x.AssignmentResultId == existingResult.Id)
                    .ToList();

                if (oldAnswers.Count > 0)
                {
                    _context.AssignmentAnswers.RemoveRange(oldAnswers);
                    _context.SaveChanges();
                }

                foreach (var answerDto in dto.Answers)
                {
                    if (answerDto == null)
                        continue;

                    var answer = new AssignmentAnswer
                    {
                        AssignmentResultId = existingResult.Id,
                        AssignmentId = dto.AssignmentId,
                        StudentId = studentId,
                        QuestionText = answerDto.QuestionText ?? string.Empty,
                        StudentAnswer = answerDto.StudentAnswer ?? string.Empty,
                        CorrectAnswer = answerDto.CorrectAnswer ?? string.Empty,
                        IsCorrect = answerDto.IsCorrect,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.AssignmentAnswers.Add(answer);
                }

                _context.SaveChanges();
            }

            return Ok(new
            {
                message = "Ödev sonucu başarıyla kaydedildi.",
                assignmentId = existingResult.AssignmentId,
                studentId = existingResult.StudentId,
                correctCount = existingResult.CorrectCount,
                wrongCount = existingResult.WrongCount,
                totalQuestionCount = existingResult.TotalQuestionCount,
                score = existingResult.Score,
                isCompleted = existingResult.IsCompleted,
                completedAt = existingResult.CompletedAt,
                answersCount = dto.Answers != null ? dto.Answers.Count : 0
            });
        }

        [HttpGet("assignment/{assignmentId:int}/completed-students")]
        public IActionResult GetCompletedStudentsByAssignment(int assignmentId)
        {
            if (assignmentId <= 0)
            {
                return BadRequest(new
                {
                    message = "AssignmentId geçersiz."
                });
            }

            var results = _context.AssignmentResults
                .Include(x => x.Student)
                .Where(x => x.AssignmentId == assignmentId && x.IsCompleted)
                .OrderByDescending(x => x.CompletedAt)
                .ToList()
                .Select(x => new CompletedStudentResultDto
                {
                    ResultId = x.Id,
                    StudentId = x.StudentId,
                    StudentName = x.Student != null ? x.Student.Name : string.Empty,
                    StudentSurname = x.Student != null ? x.Student.Surname : string.Empty,
                    CorrectCount = x.CorrectCount,
                    WrongCount = x.WrongCount,
                    TotalQuestionCount = x.TotalQuestionCount,
                    Score = x.Score,
                    CompletedAt = x.CompletedAt.ToString("yyyy-MM-ddTHH:mm:ss")
                })
                .ToList();

            return new JsonResult(results, PascalCaseJsonOptions);
        }

        [HttpGet("{resultId:int}/answers")]
        public IActionResult GetAssignmentResultAnswers(int resultId)
        {
            if (resultId <= 0)
            {
                return BadRequest(new
                {
                    message = "ResultId geçersiz."
                });
            }

            var resultExists = _context.AssignmentResults.Any(x => x.Id == resultId);

            if (!resultExists)
            {
                return NotFound(new
                {
                    message = "Ödev sonucu bulunamadı."
                });
            }

            var answers = _context.AssignmentAnswers
                .Where(x => x.AssignmentResultId == resultId)
                .OrderBy(x => x.Id)
                .Select(x => new StudentAnswerDto
                {
                    QuestionText = x.QuestionText,
                    StudentAnswer = x.StudentAnswer,
                    CorrectAnswer = x.CorrectAnswer,
                    IsCorrect = x.IsCorrect
                })
                .ToList();

            return new JsonResult(answers, PascalCaseJsonOptions);
        }

        private int GetCurrentUserId()
        {
            string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(userIdValue, out int userId))
            {
                return userId;
            }

            return 0;
        }
    }

    public class SubmitAssignmentResultDto
    {
        public int AssignmentId { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public int TotalQuestionCount { get; set; }
        public int Score { get; set; }

        public List<SubmitAssignmentAnswerDto>? Answers { get; set; }
    }

    public class SubmitAssignmentAnswerDto
    {
        public string? QuestionText { get; set; }
        public string? StudentAnswer { get; set; }
        public string? CorrectAnswer { get; set; }
        public bool IsCorrect { get; set; }
    }

    public class CompletedStudentResultDto
    {
        public int ResultId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentSurname { get; set; } = string.Empty;
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
        public int TotalQuestionCount { get; set; }
        public int Score { get; set; }
        public string CompletedAt { get; set; } = string.Empty;
    }

    public class StudentAnswerDto
    {
        public string QuestionText { get; set; } = string.Empty;
        public string StudentAnswer { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}