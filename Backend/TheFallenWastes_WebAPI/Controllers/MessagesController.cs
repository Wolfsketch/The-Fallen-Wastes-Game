using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheFallenWastes_Domain.Entities;
using TheFallenWastes_Infrastructure;

namespace TheFallenWastes_WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly GameDbContext _db;

        public MessagesController(GameDbContext db)
        {
            _db = db;
        }

        [HttpGet("inbox/{playerId}")]
        public async Task<IActionResult> GetInbox(Guid playerId)
        {
            var messages = await _db.Messages
                .Where(m => m.ReceiverPlayerId == playerId)
                .OrderByDescending(m => m.SentAtUtc)
                .ToListAsync();

            var playerIds = messages
                .Select(m => m.SenderPlayerId)
                .Concat(messages.Select(m => m.ReceiverPlayerId))
                .Distinct()
                .ToList();

            var players = await _db.Players
                .Where(p => playerIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Username);

            var result = messages.Select(m => new
            {
                m.Id,
                SenderId = m.SenderPlayerId,
                ReceiverId = m.ReceiverPlayerId,
                SenderName = players.GetValueOrDefault(m.SenderPlayerId, "Unknown"),
                ReceiverName = players.GetValueOrDefault(m.ReceiverPlayerId, "Unknown"),
                m.Subject,
                m.Body,
                m.SentAtUtc,
                m.IsRead
            });

            return Ok(result);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchPlayers([FromQuery] string q, [FromQuery] Guid? excludePlayerId = null)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 1)
                return Ok(Array.Empty<object>());

            q = q.Trim();

            var query = _db.Players.AsQueryable();

            if (excludePlayerId.HasValue && excludePlayerId.Value != Guid.Empty)
                query = query.Where(p => p.Id != excludePlayerId.Value);

            var players = await query
                .Where(p => p.Username.Contains(q))
                .OrderBy(p => p.Username)
                .Take(8)
                .Select(p => new
                {
                    p.Id,
                    p.Username
                })
                .ToListAsync();

            return Ok(players);
        }

        [HttpGet("unread-count/{playerId}")]
        public async Task<IActionResult> GetUnreadCount(Guid playerId)
        {
            var count = await _db.Messages.CountAsync(m =>
                m.ReceiverPlayerId == playerId &&
                !m.IsRead);

            return Ok(new { Count = count });
        }

        [HttpPost("mark-read/{messageId}")]
        public async Task<IActionResult> MarkAsRead(Guid messageId)
        {
            var message = await _db.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
            if (message == null)
                return NotFound("Message not found.");

            message.MarkAsRead();
            await _db.SaveChangesAsync();

            return Ok(new { Message = "Marked as read." });
        }

        [HttpGet("sent/{playerId}")]
        public async Task<IActionResult> GetSent(Guid playerId)
        {
            var messages = await _db.Messages
                .Where(m => m.SenderPlayerId == playerId)
                .OrderByDescending(m => m.SentAtUtc)
                .ToListAsync();

            var playerIds = messages
                .Select(m => m.SenderPlayerId)
                .Concat(messages.Select(m => m.ReceiverPlayerId))
                .Distinct()
                .ToList();

            var players = await _db.Players
                .Where(p => playerIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Username);

            var result = messages.Select(m => new
            {
                m.Id,
                SenderId = m.SenderPlayerId,
                ReceiverId = m.ReceiverPlayerId,
                SenderName = players.GetValueOrDefault(m.SenderPlayerId, "Unknown"),
                ReceiverName = players.GetValueOrDefault(m.ReceiverPlayerId, "Unknown"),
                m.Subject,
                m.Body,
                m.SentAtUtc,
                m.IsRead
            });

            return Ok(result);
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] SendMessageRequest request)
        {
            if (request.SenderPlayerId == Guid.Empty || request.ReceiverPlayerId == Guid.Empty)
                return BadRequest("Invalid sender or receiver.");

            if (string.IsNullOrWhiteSpace(request.Subject))
                return BadRequest("Subject is required.");

            if (string.IsNullOrWhiteSpace(request.Body))
                return BadRequest("Message body is required.");

            var senderExists = await _db.Players.AnyAsync(p => p.Id == request.SenderPlayerId);
            var receiverExists = await _db.Players.AnyAsync(p => p.Id == request.ReceiverPlayerId);

            if (!senderExists || !receiverExists)
                return BadRequest("Sender or receiver not found.");

            var message = new Message(
                request.SenderPlayerId,
                request.ReceiverPlayerId,
                request.Subject.Trim(),
                request.Body.Trim()
            );

            _db.Messages.Add(message);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message.Id,
                message.SentAtUtc,
                Message = "Message sent successfully."
            });
        }
    }

    public class SendMessageRequest
    {
        public Guid SenderPlayerId { get; set; }
        public Guid ReceiverPlayerId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}