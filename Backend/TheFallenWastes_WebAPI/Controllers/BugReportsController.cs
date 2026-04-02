using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheFallenWastes_Domain.Entities;
using TheFallenWastes_Infrastructure;

namespace TheFallenWastes_WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BugReportsController : ControllerBase
    {
        private readonly GameDbContext _context;

        public BugReportsController(GameDbContext context)
        {
            _context = context;
        }

        // POST /api/bugreports
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] SubmitBugReportRequest request)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == request.PlayerId);
            if (player == null)
                return NotFound("Player not found.");

            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
                return BadRequest("Title and Description are required.");

            var report = new BugReport(
                request.PlayerId,
                request.Title,
                request.Category ?? "Other",
                request.Area ?? string.Empty,
                request.Severity ?? "Low",
                request.Description,
                request.StepsToReproduce ?? string.Empty,
                request.SettlementName ?? string.Empty,
                request.Browser ?? string.Empty
            );

            _context.BugReports.Add(report);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                report.Id,
                report.Title,
                report.Category,
                report.Area,
                report.Severity,
                report.Status,
                report.CreatedAtUtc
            });
        }

        // GET /api/bugreports/player/{playerId}
        [HttpGet("player/{playerId}")]
        public async Task<IActionResult> GetByPlayer(Guid playerId)
        {
            var reports = await _context.BugReports
                .Where(r => r.PlayerId == playerId)
                .OrderByDescending(r => r.CreatedAtUtc)
                .Select(r => new
                {
                    r.Id,
                    r.Title,
                    r.Category,
                    r.Area,
                    r.Severity,
                    r.Status,
                    r.CreatedAtUtc
                })
                .ToListAsync();

            return Ok(reports);
        }

        // GET /api/bugreports — admin: all reports
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 30)
        {
            var query = _context.BugReports.AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(r => r.Status == status);

            var total = await query.CountAsync();
            var reports = await query
                .OrderByDescending(r => r.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    r.Id,
                    r.PlayerId,
                    r.Title,
                    r.Category,
                    r.Area,
                    r.Severity,
                    r.Description,
                    r.StepsToReproduce,
                    r.SettlementName,
                    r.Browser,
                    r.Status,
                    r.CreatedAtUtc
                })
                .ToListAsync();

            return Ok(new { total, page, pageSize, reports });
        }

        // PATCH /api/bugreports/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
        {
            var report = await _context.BugReports.FindAsync(id);
            if (report == null) return NotFound();

            var valid = new[] { "Open", "InProgress", "Resolved", "Closed" };
            if (!valid.Contains(request.Status))
                return BadRequest("Invalid status.");

            report.SetStatus(request.Status);
            await _context.SaveChangesAsync();
            return Ok(new { report.Id, report.Status });
        }
    }

    public record SubmitBugReportRequest(
        Guid PlayerId,
        string Title,
        string? Category,
        string? Area,
        string? Severity,
        string Description,
        string? StepsToReproduce,
        string? SettlementName,
        string? Browser
    );

    public record UpdateStatusRequest(string Status);
}
