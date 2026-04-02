using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheFallenWastes_Domain.Entities;
using TheFallenWastes_Infrastructure;

namespace TheFallenWastes_WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ForumController : ControllerBase
    {
        private readonly GameDbContext _context;

        public ForumController(GameDbContext context)
        {
            _context = context;
        }

        // GET /api/forum/stats — topic+post counts per category + global totals
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var topics = await _context.ForumTopics.ToListAsync();
            var postCounts = await _context.ForumPosts
                .GroupBy(p => p.TopicId)
                .Select(g => new { TopicId = g.Key, Count = g.Count() })
                .ToListAsync();

            var postByTopic = postCounts.ToDictionary(x => x.TopicId, x => x.Count);

            var byCategory = topics
                .GroupBy(t => t.CategoryKey)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        topics = g.Count(),
                        posts = g.Sum(t => postByTopic.GetValueOrDefault(t.Id, 0))
                    }
                );

            int totalTopics = topics.Count;
            int totalPosts = postCounts.Sum(x => x.Count);
            int totalMembers = await _context.Players.CountAsync();

            return Ok(new { categories = byCategory, totalTopics, totalPosts, totalMembers });
        }

        // GET /api/forum/categories — each category with latest topic info
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var allTopics = await _context.ForumTopics
                .OrderByDescending(t => t.LastPostAtUtc)
                .ToListAsync();

            var postCounts = await _context.ForumPosts
                .GroupBy(p => p.TopicId)
                .Select(g => new { TopicId = g.Key, Count = g.Count() })
                .ToListAsync();

            var postByTopic = postCounts.ToDictionary(x => x.TopicId, x => x.Count);

            var categoryKeys = new[]
            {
                "announcements", "release-notes",
                "suggestions", "ideas-vote", "general-feedback",
                "q-and-a", "guides", "strategy",
                "introductions", "off-topic", "alliance-recruitment"
            };

            var result = categoryKeys.Select(key =>
            {
                var catTopics = allTopics.Where(t => t.CategoryKey == key).ToList();
                var latest = catTopics.OrderByDescending(t => t.LastPostAtUtc).FirstOrDefault();
                int totalPosts = catTopics.Sum(t => postByTopic.GetValueOrDefault(t.Id, 0));

                return new
                {
                    key,
                    topicCount = catTopics.Count,
                    postCount = totalPosts,
                    latest = latest == null ? null : (object)new
                    {
                        topicId = latest.Id,
                        topicTitle = latest.Title,
                        username = latest.LastPostUsername,
                        at = latest.LastPostAtUtc
                    }
                };
            });

            return Ok(result);
        }

        // GET /api/forum/topics?category=suggestions&page=1&pageSize=25
        [HttpGet("topics")]
        public async Task<IActionResult> GetTopics([FromQuery] string? category, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
        {
            var query = _context.ForumTopics.AsQueryable();
            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(t => t.CategoryKey == category);

            var total = await query.CountAsync();
            var topics = await query
                .OrderByDescending(t => t.IsPinned)
                .ThenByDescending(t => t.LastPostAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var topicIds = topics.Select(t => t.Id).ToList();
            var postCounts = await _context.ForumPosts
                .Where(p => topicIds.Contains(p.TopicId))
                .GroupBy(p => p.TopicId)
                .Select(g => new { TopicId = g.Key, Count = g.Count() })
                .ToListAsync();

            var postByTopic = postCounts.ToDictionary(x => x.TopicId, x => x.Count);

            var result = topics.Select(t => new
            {
                t.Id, t.CategoryKey, t.Title, t.AuthorPlayerId, t.AuthorUsername,
                t.IsPinned, t.IsOfficial, t.CreatedAtUtc, t.LastPostAtUtc,
                t.LastPostPlayerId, t.LastPostUsername,
                postCount = postByTopic.GetValueOrDefault(t.Id, 0)
            });

            return Ok(new { total, page, pageSize, topics = result });
        }

        // GET /api/forum/topics/{id} — full topic with posts
        [HttpGet("topics/{id}")]
        public async Task<IActionResult> GetTopic(Guid id)
        {
            var topic = await _context.ForumTopics
                .Include(t => t.Posts)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (topic == null) return NotFound();

            return Ok(new
            {
                topic.Id, topic.CategoryKey, topic.Title,
                topic.AuthorPlayerId, topic.AuthorUsername,
                topic.IsPinned, topic.IsOfficial,
                topic.CreatedAtUtc, topic.LastPostAtUtc,
                posts = topic.Posts
                    .OrderBy(p => p.CreatedAtUtc)
                    .Select(p => new
                    {
                        p.Id, p.AuthorPlayerId, p.AuthorUsername, p.Content, p.CreatedAtUtc
                    })
            });
        }

        // POST /api/forum/topics — create new topic
        [HttpPost("topics")]
        public async Task<IActionResult> CreateTopic([FromBody] CreateTopicRequest request)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == request.PlayerId);
            if (player == null) return NotFound("Player not found.");
            if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest("Title is required.");
            if (string.IsNullOrWhiteSpace(request.Content)) return BadRequest("Content is required.");

            var topic = new ForumTopic(
                request.CategoryKey,
                request.Title,
                player.Id,
                player.Username,
                false
            );
            _context.ForumTopics.Add(topic);
            await _context.SaveChangesAsync();

            var post = new ForumPost(topic.Id, player.Id, player.Username, request.Content);
            _context.ForumPosts.Add(post);
            await _context.SaveChangesAsync();

            return Ok(new { topicId = topic.Id, postId = post.Id });
        }

        // POST /api/forum/topics/{id}/posts — reply to a topic
        [HttpPost("topics/{id}/posts")]
        public async Task<IActionResult> CreatePost(Guid id, [FromBody] CreatePostRequest request)
        {
            var topic = await _context.ForumTopics.FindAsync(id);
            if (topic == null) return NotFound("Topic not found.");

            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == request.PlayerId);
            if (player == null) return NotFound("Player not found.");
            if (string.IsNullOrWhiteSpace(request.Content)) return BadRequest("Content is required.");

            var post = new ForumPost(id, player.Id, player.Username, request.Content);
            _context.ForumPosts.Add(post);

            topic.UpdateLastPost(player.Id, player.Username);
            await _context.SaveChangesAsync();

            return Ok(new { post.Id, post.AuthorUsername, post.CreatedAtUtc });
        }

        // GET /api/forum/online — count of players active in the last 5 min (approximated by score > 0)
        [HttpGet("online")]
        public async Task<IActionResult> GetOnline()
        {
            // We don't have a last-seen timestamp, so return total active player count as approximation
            var players = await _context.Players
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Score)
                .Take(20)
                .Select(p => p.Username)
                .ToListAsync();

            return Ok(new { count = players.Count, names = players });
        }
    }

    public record CreateTopicRequest(Guid PlayerId, string CategoryKey, string Title, string Content);
    public record CreatePostRequest(Guid PlayerId, string Content);
}
