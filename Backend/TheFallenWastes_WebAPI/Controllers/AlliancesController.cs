using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheFallenWastes_Domain.Entities;
using TheFallenWastes_Domain.Enums;
using TheFallenWastes_Infrastructure;

namespace TheFallenWastes_WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlliancesController : ControllerBase
    {
        private readonly GameDbContext _context;

        public AlliancesController(GameDbContext context)
        {
            _context = context;
        }

        // ════════════════════════════════════════════════════════
        // GET /api/alliances  — list all alliances (browse)
        // ════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> ListAlliances()
        {
            var alliances = await _context.Alliances
                .Include(a => a.Members)
                .OrderByDescending(a => a.Members.Count)
                .ToListAsync();

            var result = await Task.WhenAll(alliances.Select(async a =>
            {
                int totalScore = 0;
                foreach (var m in a.Members)
                {
                    var p = await _context.Players.FirstOrDefaultAsync(x => x.Id == m.PlayerId);
                    if (p != null) totalScore += p.Score;
                }
                return new
                {
                    a.Id, a.Name, a.Tag, a.Description, a.Status, a.MinPoints,
                    a.FoundedAtUtc, MemberCount = a.Members.Count, TotalScore = totalScore
                };
            }));

            return Ok(result.OrderByDescending(x => x.TotalScore));
        }

        // ════════════════════════════════════════════════════════
        // GET /api/alliances/{id}  — get alliance details
        // ════════════════════════════════════════════════════════
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAlliance(Guid id)
        {
            var alliance = await _context.Alliances
                .Include(a => a.Members)
                .Include(a => a.Applications)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (alliance == null)
                return NotFound("Alliance not found.");

            var memberDetails = new List<object>();
            foreach (var m in alliance.Members.OrderBy(m => m.Rank).ThenBy(m => m.JoinedAtUtc))
            {
                var p = await _context.Players
                    .Include(x => x.Settlements)
                    .FirstOrDefaultAsync(x => x.Id == m.PlayerId);
                if (p != null)
                {
                    memberDetails.Add(new
                    {
                        m.Id, PlayerId = p.Id, Username = p.Username,
                        Score = p.Score, SettlementCount = p.Settlements.Count,
                        m.Rank, m.JoinedAtUtc
                    });
                }
            }

            var pendingApps = alliance.Applications
                .Where(a => a.Status == "Pending" && !a.IsInvitation)
                .Select(a => new { a.Id, a.PlayerId, a.Message, a.CreatedAtUtc })
                .ToList();

            var pendingInvites = alliance.Applications
                .Where(a => a.Status == "Pending" && a.IsInvitation)
                .Select(a => new { a.Id, a.PlayerId, a.Message, a.CreatedAtUtc })
                .ToList();

            return Ok(new
            {
                alliance.Id, alliance.Name, alliance.Tag, alliance.Description,
                alliance.Status, alliance.MinPoints, alliance.FoundedAtUtc,
                alliance.FounderPlayerId,
                MemberCount = alliance.Members.Count,
                Members = memberDetails,
                PendingApplications = pendingApps,
                PendingInvitations = pendingInvites
            });
        }

        // ════════════════════════════════════════════════════════
        // GET /api/alliances/player/{playerId}  — get player's alliance
        // ════════════════════════════════════════════════════════
        [HttpGet("player/{playerId}")]
        public async Task<IActionResult> GetPlayerAlliance(Guid playerId)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == playerId);
            if (player == null) return NotFound("Player not found.");
            if (player.AllianceId == null) return Ok(null);

            return await GetAlliance(player.AllianceId.Value);
        }

        // ════════════════════════════════════════════════════════
        // POST /api/alliances  — create alliance
        // ════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> CreateAlliance([FromBody] CreateAllianceRequest request)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == request.PlayerId);
            if (player == null) return NotFound("Player not found.");
            if (player.AllianceId != null) return BadRequest("You are already in an alliance.");

            var nameExists = await _context.Alliances.AnyAsync(a => a.Name == request.Name.Trim());
            if (nameExists) return BadRequest("Alliance name already taken.");

            var tagUpper = request.Tag.Trim().ToUpperInvariant();
            var tagExists = await _context.Alliances.AnyAsync(a => a.Tag == tagUpper);
            if (tagExists) return BadRequest("Alliance tag already taken.");

            try
            {
                var alliance = new Alliance(request.Name, request.Tag, request.PlayerId);
                _context.Alliances.Add(alliance);

                var member = new AllianceMember(alliance.Id, request.PlayerId, AllianceMemberRank.Founder);
                _context.AllianceMembers.Add(member);

                player.JoinAlliance(alliance.Id);

                await _context.SaveChangesAsync();

                return Ok(new { alliance.Id, alliance.Name, alliance.Tag });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ════════════════════════════════════════════════════════
        // POST /api/alliances/{id}/apply  — player applies to join
        // ════════════════════════════════════════════════════════
        [HttpPost("{id}/apply")]
        public async Task<IActionResult> ApplyToJoin(Guid id, [FromBody] AllianceApplicationRequest request)
        {
            var alliance = await _context.Alliances.FirstOrDefaultAsync(a => a.Id == id);
            if (alliance == null) return NotFound("Alliance not found.");
            if (alliance.Status == AllianceStatus.Closed) return BadRequest("This alliance is closed.");

            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == request.PlayerId);
            if (player == null) return NotFound("Player not found.");
            if (player.AllianceId != null) return BadRequest("You are already in an alliance.");
            if (player.Score < alliance.MinPoints)
                return BadRequest($"You need at least {alliance.MinPoints} points to join this alliance.");

            var existing = await _context.AllianceApplications
                .AnyAsync(a => a.AllianceId == id && a.PlayerId == request.PlayerId && a.Status == "Pending");
            if (existing) return BadRequest("You already have a pending application.");

            if (alliance.Status == AllianceStatus.Open)
            {
                // Auto-accept
                var member = new AllianceMember(id, request.PlayerId, AllianceMemberRank.Member);
                _context.AllianceMembers.Add(member);
                player.JoinAlliance(id);
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Joined alliance." });
            }

            var app = new AllianceApplication(id, request.PlayerId, request.Message ?? string.Empty);
            _context.AllianceApplications.Add(app);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Application submitted." });
        }

        // ════════════════════════════════════════════════════════
        // POST /api/alliances/{id}/invite — invite a player
        // ════════════════════════════════════════════════════════
        [HttpPost("{id}/invite")]
        public async Task<IActionResult> InvitePlayer(Guid id, [FromBody] AllianceInviteRequest request)
        {
            var alliance = await _context.Alliances.FirstOrDefaultAsync(a => a.Id == id);
            if (alliance == null) return NotFound("Alliance not found.");

            var requester = await _context.Players.FirstOrDefaultAsync(p => p.Id == request.RequesterId);
            if (requester == null || requester.AllianceId != id) return Forbid();

            var reqMember = await _context.AllianceMembers
                .FirstOrDefaultAsync(m => m.AllianceId == id && m.PlayerId == request.RequesterId);
            if (reqMember == null || (reqMember.Rank > AllianceMemberRank.Officer && !reqMember.CanInvite)) return Forbid();

            var target = await _context.Players.FirstOrDefaultAsync(p => p.Id == request.TargetPlayerId);
            if (target == null) return NotFound("Target player not found.");
            if (target.AllianceId != null) return BadRequest("Player is already in an alliance.");

            var existing = await _context.AllianceApplications
                .AnyAsync(a => a.AllianceId == id && a.PlayerId == request.TargetPlayerId && a.Status == "Pending" && a.IsInvitation);
            if (existing) return BadRequest("Player already has a pending invitation.");

            var invite = new AllianceApplication(id, request.TargetPlayerId, request.Message ?? string.Empty, isInvitation: true);
            _context.AllianceApplications.Add(invite);

            var msgBody = $"You have been invited to join [{alliance.Tag}] {alliance.Name}.\n\n" +
                          (!string.IsNullOrWhiteSpace(request.Message) ? $"\"{request.Message}\"\n\n" : "") +
                          "Open the Alliance tab → INVITATIONS to respond.";
            var mailMessage = new Message(
                request.RequesterId,
                request.TargetPlayerId,
                $"Faction Invitation: [{alliance.Tag}] {alliance.Name}",
                msgBody,
                "alliance_invite");
            _context.Messages.Add(mailMessage);

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Invitation sent." });
        }

        // ════════════════════════════════════════════════════════
        // POST /api/alliances/{id}/applications/{appId}/accept
        // ════════════════════════════════════════════════════════
        [HttpPost("{id}/applications/{appId}/accept")]
        public async Task<IActionResult> AcceptApplication(Guid id, Guid appId, [FromBody] AllianceActionRequest request)
        {
            var app = await _context.AllianceApplications.FirstOrDefaultAsync(a => a.Id == appId && a.AllianceId == id);
            if (app == null) return NotFound();
            if (app.Status != "Pending") return BadRequest("Application is no longer pending.");

            var reqMember = await _context.AllianceMembers
                .FirstOrDefaultAsync(m => m.AllianceId == id && m.PlayerId == request.RequesterId);
            if (reqMember == null || (reqMember.Rank > AllianceMemberRank.Officer && !reqMember.CanManageRecruitment)) return Forbid();

            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == app.PlayerId);
            if (player == null) return NotFound("Player not found.");
            if (player.AllianceId != null) return BadRequest("Player is already in an alliance.");

            app.Accept();
            var member = new AllianceMember(id, app.PlayerId, AllianceMemberRank.Member);
            _context.AllianceMembers.Add(member);
            player.JoinAlliance(id);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Application accepted." });
        }

        // ════════════════════════════════════════════════════════
        // POST /api/alliances/{id}/applications/{appId}/reject
        // ════════════════════════════════════════════════════════
        [HttpPost("{id}/applications/{appId}/reject")]
        public async Task<IActionResult> RejectApplication(Guid id, Guid appId, [FromBody] AllianceActionRequest request)
        {
            var app = await _context.AllianceApplications.FirstOrDefaultAsync(a => a.Id == appId && a.AllianceId == id);
            if (app == null) return NotFound();
            if (app.Status != "Pending") return BadRequest("Application is no longer pending.");

            var reqMember = await _context.AllianceMembers
                .FirstOrDefaultAsync(m => m.AllianceId == id && m.PlayerId == request.RequesterId);
            if (reqMember == null || reqMember.Rank > AllianceMemberRank.Officer) return Forbid();

            app.Reject();
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Application rejected." });
        }

        // ════════════════════════════════════════════════════════
        // POST /api/alliances/{id}/invitations/{appId}/accept  — player accepts invite
        // ════════════════════════════════════════════════════════
        [HttpPost("{id}/invitations/{appId}/accept")]
        public async Task<IActionResult> AcceptInvitation(Guid id, Guid appId, [FromBody] AllianceSelfActionRequest request)
        {
            var invite = await _context.AllianceApplications
                .FirstOrDefaultAsync(a => a.Id == appId && a.AllianceId == id && a.IsInvitation && a.PlayerId == request.PlayerId);
            if (invite == null) return NotFound();
            if (invite.Status != "Pending") return BadRequest("Invitation is no longer pending.");

            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == request.PlayerId);
            if (player == null) return NotFound("Player not found.");
            if (player.AllianceId != null) return BadRequest("You are already in an alliance.");

            invite.Accept();
            var member = new AllianceMember(id, request.PlayerId, AllianceMemberRank.Member);
            _context.AllianceMembers.Add(member);
            player.JoinAlliance(id);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Invitation accepted." });
        }

        // ════════════════════════════════════════════════════════
        // POST /api/alliances/{id}/invitations/{appId}/reject  — player rejects invite
        // ════════════════════════════════════════════════════════
        [HttpPost("{id}/invitations/{appId}/reject")]
        public async Task<IActionResult> RejectInvitation(Guid id, Guid appId, [FromBody] AllianceSelfActionRequest request)
        {
            var invite = await _context.AllianceApplications
                .FirstOrDefaultAsync(a => a.Id == appId && a.AllianceId == id && a.IsInvitation && a.PlayerId == request.PlayerId);
            if (invite == null) return NotFound();
            if (invite.Status != "Pending") return BadRequest("Invitation is no longer pending.");

            invite.Reject();
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Invitation rejected." });
        }

        // ════════════════════════════════════════════════════════
        // GET /api/alliances/{id}/invitations/player/{playerId}  — get player's pending invites
        // ════════════════════════════════════════════════════════
        [HttpGet("{id}/invitations/player/{playerId}")]
        public async Task<IActionResult> GetPlayerInvitations(Guid id, Guid playerId)
        {
            var invites = await _context.AllianceApplications
                .Where(a => a.AllianceId == id && a.PlayerId == playerId && a.IsInvitation && a.Status == "Pending")
                .ToListAsync();

            return Ok(invites.Select(i => new { i.Id, i.Message, i.CreatedAtUtc }));
        }

        // ════════════════════════════════════════════════════════
        // GET /api/alliances/invitations/player/{playerId}  — all invites for player
        // ════════════════════════════════════════════════════════
        [HttpGet("invitations/player/{playerId}")]
        public async Task<IActionResult> GetAllPlayerInvitations(Guid playerId)
        {
            var invites = await _context.AllianceApplications
                .Where(a => a.PlayerId == playerId && a.IsInvitation && a.Status == "Pending")
                .ToListAsync();

            var result = new List<object>();
            foreach (var inv in invites)
            {
                var alliance = await _context.Alliances.FirstOrDefaultAsync(a => a.Id == inv.AllianceId);
                if (alliance != null)
                    result.Add(new { inv.Id, AllianceId = alliance.Id, AllianceName = alliance.Name, AllianceTag = alliance.Tag, inv.Message, inv.CreatedAtUtc });
            }
            return Ok(result);
        }

        // ════════════════════════════════════════════════════════
        // GET /api/alliances/applications/player/{playerId} — player's pending applications
        // ════════════════════════════════════════════════════════
        [HttpGet("applications/player/{playerId}")]
        public async Task<IActionResult> GetPlayerApplications(Guid playerId)
        {
            var apps = await _context.AllianceApplications
                .Where(a => a.PlayerId == playerId && !a.IsInvitation && a.Status == "Pending")
                .ToListAsync();

            var result = new List<object>();
            foreach (var app in apps)
            {
                var alliance = await _context.Alliances.FirstOrDefaultAsync(a => a.Id == app.AllianceId);
                if (alliance != null)
                    result.Add(new { app.Id, AllianceId = alliance.Id, AllianceName = alliance.Name, AllianceTag = alliance.Tag, app.Message, app.CreatedAtUtc });
            }
            return Ok(result);
        }

        // ════════════════════════════════════════════════════════
        // POST /api/alliances/{id}/kick/{playerId}
        // ════════════════════════════════════════════════════════
        [HttpPost("{id}/kick/{targetPlayerId}")]
        public async Task<IActionResult> KickMember(Guid id, Guid targetPlayerId, [FromBody] AllianceActionRequest request)
        {
            var reqMember = await _context.AllianceMembers
                .FirstOrDefaultAsync(m => m.AllianceId == id && m.PlayerId == request.RequesterId);
            if (reqMember == null || reqMember.Rank > AllianceMemberRank.Officer) return Forbid();

            var targetMember = await _context.AllianceMembers
                .FirstOrDefaultAsync(m => m.AllianceId == id && m.PlayerId == targetPlayerId);
            if (targetMember == null) return NotFound("Member not found.");
            if (targetMember.Rank <= reqMember.Rank) return BadRequest("Cannot kick a member of equal or higher rank.");

            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == targetPlayerId);
            if (player != null) player.LeaveAlliance();

            _context.AllianceMembers.Remove(targetMember);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Member kicked." });
        }

        // ════════════════════════════════════════════════════════
        // POST /api/alliances/{id}/leave
        // ════════════════════════════════════════════════════════
        [HttpPost("{id}/leave")]
        public async Task<IActionResult> LeaveAlliance(Guid id, [FromBody] AllianceSelfActionRequest request)
        {
            var member = await _context.AllianceMembers
                .FirstOrDefaultAsync(m => m.AllianceId == id && m.PlayerId == request.PlayerId);
            if (member == null) return NotFound("You are not in this alliance.");
            if (member.Rank == AllianceMemberRank.Founder) return BadRequest("Founder cannot leave. Transfer leadership or dissolve the alliance.");

            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == request.PlayerId);
            if (player != null) player.LeaveAlliance();

            _context.AllianceMembers.Remove(member);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Left alliance." });
        }

        // ════════════════════════════════════════════════════════
        // DELETE /api/alliances/{id}  — dissolve alliance
        // ════════════════════════════════════════════════════════
        [HttpDelete("{id}")]
        public async Task<IActionResult> DissolveAlliance(Guid id, [FromBody] AllianceSelfActionRequest request)
        {
            var alliance = await _context.Alliances
                .Include(a => a.Members)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (alliance == null) return NotFound("Alliance not found.");
            if (alliance.FounderPlayerId != request.PlayerId) return Forbid();

            // Remove all members from the alliance
            var memberIds = alliance.Members.Select(m => m.PlayerId).ToList();
            foreach (var memberId in memberIds)
            {
                var p = await _context.Players.FirstOrDefaultAsync(x => x.Id == memberId);
                if (p != null) p.LeaveAlliance();
            }

            _context.Alliances.Remove(alliance);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Alliance dissolved." });
        }

        // ════════════════════════════════════════════════════════
        // PUT /api/alliances/{id}/settings
        // ════════════════════════════════════════════════════════
        [HttpPut("{id}/settings")]
        public async Task<IActionResult> UpdateSettings(Guid id, [FromBody] UpdateAllianceSettingsRequest request)
        {
            var alliance = await _context.Alliances.FirstOrDefaultAsync(a => a.Id == id);
            if (alliance == null) return NotFound("Alliance not found.");

            var reqMember = await _context.AllianceMembers
                .FirstOrDefaultAsync(m => m.AllianceId == id && m.PlayerId == request.RequesterId);
            if (reqMember == null || reqMember.Rank > AllianceMemberRank.Leader) return Forbid();

            try
            {
                alliance.UpdateSettings(request.Name, request.Tag, request.Description, request.Status, request.MinPoints);
                await _context.SaveChangesAsync();
                return Ok(new { alliance.Id, alliance.Name, alliance.Tag, alliance.Description, alliance.Status, alliance.MinPoints });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // ════════════════════════════════════════════════════════
        // POST /api/alliances/{id}/members/{memberId}/rank
        // ════════════════════════════════════════════════════════
        [HttpPost("{id}/members/{targetPlayerId}/rank")]
        public async Task<IActionResult> SetMemberRank(Guid id, Guid targetPlayerId, [FromBody] SetRankRequest request)
        {
            var reqMember = await _context.AllianceMembers
                .FirstOrDefaultAsync(m => m.AllianceId == id && m.PlayerId == request.RequesterId);
            if (reqMember == null || reqMember.Rank > AllianceMemberRank.Leader) return Forbid();

            var target = await _context.AllianceMembers
                .FirstOrDefaultAsync(m => m.AllianceId == id && m.PlayerId == targetPlayerId);
            if (target == null) return NotFound("Member not found.");
            if (target.Rank == AllianceMemberRank.Founder) return BadRequest("Cannot change Founder rank.");

            target.SetRank(request.Rank);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Rank updated." });
        }

        // ════════════════════════════════════════════════════════
        // FORUM ENDPOINTS
        // ════════════════════════════════════════════════════════

        [HttpGet("{id}/forum")]
        public async Task<IActionResult> GetForumTopics(Guid id)
        {
            var topics = await _context.AllianceForumTopics
                .Where(t => t.AllianceId == id)
                .Include(t => t.Posts)
                .OrderByDescending(t => t.IsPinned)
                .ThenByDescending(t => t.CreatedAtUtc)
                .ToListAsync();

            var result = new List<object>();
            foreach (var t in topics)
            {
                var author = await _context.Players.FirstOrDefaultAsync(p => p.Id == t.AuthorPlayerId);
                var lastPost = t.Posts.OrderByDescending(p => p.CreatedAtUtc).FirstOrDefault();
                string? lastAuthorName = null;
                if (lastPost != null)
                {
                    var lastAuthor = await _context.Players.FirstOrDefaultAsync(p => p.Id == lastPost.AuthorPlayerId);
                    lastAuthorName = lastAuthor?.Username;
                }
                result.Add(new
                {
                    t.Id, t.Title, t.IsPinned, t.CreatedAtUtc,
                    AuthorName = author?.Username,
                    ReplyCount = t.Posts.Count,
                    LastPostAt = lastPost?.CreatedAtUtc,
                    LastPostAuthor = lastAuthorName
                });
            }
            return Ok(result);
        }

        [HttpPost("{id}/forum")]
        public async Task<IActionResult> CreateTopic(Guid id, [FromBody] CreateForumTopicRequest request)
        {
            var member = await _context.AllianceMembers
                .FirstOrDefaultAsync(m => m.AllianceId == id && m.PlayerId == request.AuthorPlayerId);
            if (member == null) return Forbid();

            try
            {
                var topic = new AllianceForumTopic(id, request.Title, request.AuthorPlayerId);
                _context.AllianceForumTopics.Add(topic);

                if (!string.IsNullOrWhiteSpace(request.Content))
                {
                    var post = new AllianceForumPost(topic.Id, request.AuthorPlayerId, request.Content);
                    _context.AllianceForumPosts.Add(post);
                }

                await _context.SaveChangesAsync();
                return Ok(new { topic.Id, topic.Title });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}/forum/{topicId}")]
        public async Task<IActionResult> GetTopicPosts(Guid id, Guid topicId)
        {
            var topic = await _context.AllianceForumTopics
                .Include(t => t.Posts)
                .FirstOrDefaultAsync(t => t.Id == topicId && t.AllianceId == id);
            if (topic == null) return NotFound("Topic not found.");

            var posts = new List<object>();
            foreach (var p in topic.Posts.OrderBy(p => p.CreatedAtUtc))
            {
                var author = await _context.Players.FirstOrDefaultAsync(x => x.Id == p.AuthorPlayerId);
                posts.Add(new
                {
                    p.Id, p.Content, p.CreatedAtUtc, p.EditedAtUtc,
                    AuthorName = author?.Username, AuthorId = p.AuthorPlayerId
                });
            }

            var topicAuthor = await _context.Players.FirstOrDefaultAsync(x => x.Id == topic.AuthorPlayerId);
            return Ok(new
            {
                topic.Id, topic.Title, topic.IsPinned, topic.CreatedAtUtc,
                AuthorName = topicAuthor?.Username,
                Posts = posts
            });
        }

        [HttpPost("{id}/forum/{topicId}/posts")]
        public async Task<IActionResult> AddPost(Guid id, Guid topicId, [FromBody] AddForumPostRequest request)
        {
            var member = await _context.AllianceMembers
                .FirstOrDefaultAsync(m => m.AllianceId == id && m.PlayerId == request.AuthorPlayerId);
            if (member == null) return Forbid();

            var topic = await _context.AllianceForumTopics
                .FirstOrDefaultAsync(t => t.Id == topicId && t.AllianceId == id);
            if (topic == null) return NotFound("Topic not found.");

            try
            {
                var post = new AllianceForumPost(topicId, request.AuthorPlayerId, request.Content);
                _context.AllianceForumPosts.Add(post);
                await _context.SaveChangesAsync();
                var author = await _context.Players.FirstOrDefaultAsync(p => p.Id == request.AuthorPlayerId);
                return Ok(new { post.Id, post.Content, post.CreatedAtUtc, AuthorName = author?.Username });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}/forum/{topicId}/posts/{postId}")]
        public async Task<IActionResult> DeletePost(Guid id, Guid topicId, Guid postId, [FromBody] AllianceSelfActionRequest request)
        {
            var post = await _context.AllianceForumPosts
                .FirstOrDefaultAsync(p => p.Id == postId && p.TopicId == topicId);
            if (post == null) return NotFound();

            // Allow author or officers+
            var member = await _context.AllianceMembers
                .FirstOrDefaultAsync(m => m.AllianceId == id && m.PlayerId == request.PlayerId);
            if (member == null) return Forbid();
            if (post.AuthorPlayerId != request.PlayerId && member.Rank > AllianceMemberRank.Officer && !member.IsForumModerator) return Forbid();

            _context.AllianceForumPosts.Remove(post);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Post deleted." });
        }

        [HttpDelete("{id}/forum/{topicId}")]
        public async Task<IActionResult> DeleteTopic(Guid id, Guid topicId, [FromBody] AllianceSelfActionRequest request)
        {
            var topic = await _context.AllianceForumTopics
                .FirstOrDefaultAsync(t => t.Id == topicId && t.AllianceId == id);
            if (topic == null) return NotFound("Topic not found.");

            var member = await _context.AllianceMembers
                .FirstOrDefaultAsync(m => m.AllianceId == id && m.PlayerId == request.PlayerId);
            if (member == null) return Forbid();
            if (topic.AuthorPlayerId != request.PlayerId && member.Rank > AllianceMemberRank.Officer && !member.IsForumModerator) return Forbid();

            _context.AllianceForumTopics.Remove(topic);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Topic deleted." });
        }
        // ════════════════════════════════════════════════════════
        // PATCH /api/alliances/{id}/members/{memberId}/permissions
        // ════════════════════════════════════════════════════════
        [HttpPatch("{id}/members/{memberId}/permissions")]
        public async Task<IActionResult> SetMemberPermissions(Guid id, Guid memberId, [FromBody] SetMemberPermissionsRequest request)
        {
            var reqMember = await _context.AllianceMembers
                .FirstOrDefaultAsync(m => m.AllianceId == id && m.PlayerId == request.RequesterId);
            if (reqMember == null || reqMember.Rank > AllianceMemberRank.Leader) return Forbid();

            var target = await _context.AllianceMembers
                .FirstOrDefaultAsync(m => m.AllianceId == id && m.PlayerId == memberId);
            if (target == null) return NotFound();
            if (target.Rank <= AllianceMemberRank.Officer)
                return BadRequest("Officers and above have full permissions by rank.");

            target.SetPermissions(request.CanInvite, request.CanManageRecruitment, request.IsForumModerator, request.CanBroadcast, request.CanManageReservations);
            await _context.SaveChangesAsync();
            return Ok();
        }    }

    // ═══════════════════════════════
    // Request DTOs
    // ═══════════════════════════════

    public record CreateAllianceRequest(Guid PlayerId, string Name, string Tag);
    public record AllianceApplicationRequest(Guid PlayerId, string? Message);
    public record AllianceInviteRequest(Guid RequesterId, Guid TargetPlayerId, string? Message);
    public record AllianceActionRequest(Guid RequesterId);
    public record AllianceSelfActionRequest(Guid PlayerId);
    public record UpdateAllianceSettingsRequest(
        Guid RequesterId, string Name, string Tag, string Description,
        AllianceStatus Status, int MinPoints);
    public record SetRankRequest(Guid RequesterId, AllianceMemberRank Rank);
    public record CreateForumTopicRequest(Guid AuthorPlayerId, string Title, string? Content);
    public record AddForumPostRequest(Guid AuthorPlayerId, string Content);
    public record SetMemberPermissionsRequest(Guid RequesterId, bool CanInvite, bool CanManageRecruitment, bool IsForumModerator, bool CanBroadcast, bool CanManageReservations);
}
