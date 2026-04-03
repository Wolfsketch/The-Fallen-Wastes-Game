using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheFallenWastes_Application;
using TheFallenWastes_Domain.Entities;
using TheFallenWastes_Domain.Enums;
using TheFallenWastes_Domain.GameData;
using TheFallenWastes_Infrastructure;

namespace TheFallenWastes_WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayersController : ControllerBase
    {
        private readonly GameDbContext _context;
        private readonly PlayerDataMigrationService _migrationService;

        public PlayersController(GameDbContext context, PlayerDataMigrationService migrationService)
        {
            _context = context;
            _migrationService = migrationService;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePlayer([FromBody] CreatePlayerRequest request)
        {
            var existing = await _context.Players
                .FirstOrDefaultAsync(p => p.Username == request.Username);

            if (existing != null)
                return BadRequest("Username already exists.");

            try
            {
                var player = new Player(request.Username, request.Email);
                var settlement = new Settlement(request.SettlementName, player.Id);
                player.AddSettlement(settlement);

                _context.Players.Add(player);

                foreach (var buildingType in BuildingDefinitions.StarterBuildings)
                {
                    var building = Building.CreateAtLevel(settlement.Id, buildingType, 1);
                    _context.Buildings.Add(building);
                }

                await _context.SaveChangesAsync();

                return Ok(MapPlayerResponse(player, includeSettlements: true));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPlayer(Guid id)
        {
            var player = await _context.Players
                .Include(p => p.Settlements)
                    .ThenInclude(s => s.Buildings)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (player == null)
                return NotFound("Player not found.");

            var settlement = player.Settlements.FirstOrDefault();
            bool wasMigrated = _migrationService.MigratePlayer(player, settlement, _context);

            if (wasMigrated)
                await _context.SaveChangesAsync();

            return Ok(MapPlayerResponse(player, includeSettlements: true, wasMigrated));
        }

        [HttpGet("login/{username}")]
        public async Task<IActionResult> LoginByUsername(string username)
        {
            try
            {
                var player = await _context.Players
                    .Include(p => p.Settlements)
                        .ThenInclude(s => s.Buildings)
                    .FirstOrDefaultAsync(p => p.Username == username);

                if (player == null)
                    return NotFound("Player not found.");

                if (!player.IsActive)
                    return BadRequest("Player account is deactivated.");

                var settlement = player.Settlements.FirstOrDefault();
                bool wasMigrated = _migrationService.MigratePlayer(player, settlement, _context);

                if (wasMigrated)
                    await _context.SaveChangesAsync();

                return Ok(MapPlayerResponse(player, includeSettlements: true, wasMigrated));
            }
            catch (Exception ex)
            {
                Console.WriteLine("LOGIN ERROR:");
                Console.WriteLine(ex.ToString());
                return StatusCode(500, ex.ToString());
            }
        }

        // ════════════════════════════════════════════
        // ADVISOR ENDPOINTS
        // ════════════════════════════════════════════

        [HttpGet("{id}/advisors")]
        public async Task<IActionResult> GetAdvisors(Guid id)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id);

            if (player == null)
                return NotFound("Player not found.");

            return Ok(MapAdvisors(player));
        }

        [HttpPost("{id}/advisors/{advisorId}/activate")]
        public async Task<IActionResult> ActivateAdvisor(Guid id, string advisorId, [FromBody] ActivateAdvisorRequest? request)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id);

            if (player == null)
                return NotFound("Player not found.");

            var durationDays = request?.DurationDays > 0 ? request.DurationDays : 14;
            var duration = TimeSpan.FromDays(durationDays);

            const int advisorCost = 100;
            if (player.WastelandCoins < advisorCost)
                return BadRequest($"Not enough Wasteland Cola. You need {advisorCost} Cola to activate an advisor.");

            switch (advisorId.Trim().ToLowerInvariant())
            {
                case "commander":
                    player.ActivateCommander(duration);
                    break;

                case "quartermaster":
                    player.ActivateQuartermaster(duration);
                    break;

                case "techpriest":
                case "tech-priest":
                case "tech_priest":
                    player.ActivateTechPriest(duration);
                    break;

                case "warlord":
                    player.ActivateWarlord(duration);
                    break;

                case "scoutmaster":
                case "scout-master":
                case "scout_master":
                    player.ActivateScoutMaster(duration);
                    break;

                default:
                    return BadRequest("Unknown advisor.");
            }

            player.SpendWastelandCoins(advisorCost);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = $"{advisorId} activated for {durationDays} days.",
                player.WastelandCoins,
                Advisors = MapAdvisors(player)
            });
        }

        [HttpPost("{id}/advisors/{advisorId}/deactivate")]
        public async Task<IActionResult> DeactivateAdvisor(Guid id, string advisorId)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id);

            if (player == null)
                return NotFound("Player not found.");

            switch (advisorId.Trim().ToLowerInvariant())
            {
                case "commander":
                    player.DeactivateCommander();
                    break;

                case "quartermaster":
                    player.DeactivateQuartermaster();
                    break;

                case "techpriest":
                case "tech-priest":
                case "tech_priest":
                    player.DeactivateTechPriest();
                    break;

                case "warlord":
                    player.DeactivateWarlord();
                    break;

                case "scoutmaster":
                case "scout-master":
                case "scout_master":
                    player.DeactivateScoutMaster();
                    break;

                default:
                    return BadRequest("Unknown advisor.");
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = $"{advisorId} deactivated.",
                Advisors = MapAdvisors(player)
            });
        }

        // ════════════════════════════════════════════
        // PURCHASE WASTELAND COINS
        // ════════════════════════════════════════════

        private static readonly Dictionary<string, int> _coinPackages = new(StringComparer.OrdinalIgnoreCase)
        {
            ["starter"]    = 500,
            ["scout"]      = 1_500,
            ["commander"]  = 4_000,
            ["warlord"]    = 12_500,
            ["overlord"]   = 25_000,
        };

        [HttpPost("{id}/purchase-coins")]
        public async Task<IActionResult> PurchaseCoins(Guid id, [FromBody] PurchaseCoinsRequest request)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id);
            if (player == null) return NotFound("Player not found.");

            if (!_coinPackages.TryGetValue(request.PackageId ?? "", out int amount))
                return BadRequest("Unknown package. Valid: starter, scout, commander, warlord, overlord.");

            player.AddWastelandCoins(amount);
            await _context.SaveChangesAsync();

            return Ok(new { player.WastelandCoins, Granted = amount });
        }

        // ════════════════════════════════════════════
        // TRIUMPH (WAR RAID) ENDPOINT
        // ════════════════════════════════════════════

        [HttpPost("{id}/triumph")]
        public async Task<IActionResult> OrganizeTriumph(Guid id)
        {
            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id);

            if (player == null)
                return NotFound("Player not found.");

            const int triumphCost = 300;

            if (!player.SpendWarPoints(triumphCost))
                return BadRequest($"Not enough available war points. A Triumph costs {triumphCost} BP.");

            player.AddTriumphPoints(1);
            _context.Entry(player).Property(p => p.AvailableWarPoints).IsModified = true;
            _context.Entry(player).Property(p => p.TriumphPoints).IsModified = true;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Triumph organized.",
                player.AvailableWarPoints,
                player.TriumphPoints
            });
        }

        // ════════════════════════════════════════════
        // DIPLOMACY ENDPOINTS
        // ════════════════════════════════════════════

        [HttpPost("{id}/relations")]
        public async Task<IActionResult> SetRelation(Guid id, [FromBody] SetRelationRequest request)
        {
            if (!Enum.TryParse<RelationType>(request.Type, true, out var relationType))
                return BadRequest("Invalid relation type. Use 'Friend' or 'Enemy'.");

            if (!await _context.Players.AnyAsync(p => p.Id == id))
                return NotFound("Player not found.");

            if (!Guid.TryParse(request.TargetPlayerId, out var targetId))
                return BadRequest("Invalid target player ID.");

            if (id == targetId)
                return BadRequest("Cannot set a relation with yourself.");

            if (!await _context.Players.AnyAsync(p => p.Id == targetId))
                return NotFound("Target player not found.");

            var existing = await _context.PlayerRelations
                .FirstOrDefaultAsync(r => r.PlayerId == id && r.TargetPlayerId == targetId);

            if (existing != null)
            {
                existing.ChangeType(relationType);
            }
            else
            {
                var relation = new PlayerRelation(id, targetId, relationType);
                _context.PlayerRelations.Add(relation);
            }

            await _context.SaveChangesAsync();

            var targetPlayer = await _context.Players.FindAsync(targetId);
            return Ok(new
            {
                TargetPlayerId = targetId,
                TargetUsername = targetPlayer?.Username,
                Type = relationType.ToString(),
                Message = $"Marked {targetPlayer?.Username} as {relationType}."
            });
        }

        [HttpDelete("{id}/relations/{targetPlayerId}")]
        public async Task<IActionResult> RemoveRelation(Guid id, Guid targetPlayerId)
        {
            var relation = await _context.PlayerRelations
                .FirstOrDefaultAsync(r => r.PlayerId == id && r.TargetPlayerId == targetPlayerId);

            if (relation == null)
                return BadRequest("No relation exists with this player.");

            _context.PlayerRelations.Remove(relation);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Relation removed. Player is now neutral." });
        }

        [HttpGet("{id}/relations")]
        public async Task<IActionResult> GetRelations(Guid id)
        {
            var relations = await _context.PlayerRelations
                .Where(r => r.PlayerId == id)
                .ToListAsync();

            var targetIds = relations.Select(r => r.TargetPlayerId).ToList();
            var players = await _context.Players
                .Where(p => targetIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var result = relations.Select(r =>
            {
                players.TryGetValue(r.TargetPlayerId, out var target);
                return new
                {
                    r.TargetPlayerId,
                    TargetUsername = target?.Username ?? "Unknown",
                    Type = r.Type.ToString()
                };
            });

            return Ok(result);
        }

        [HttpGet("ranking")]
        public async Task<IActionResult> GetRanking([FromQuery] string type = "total")
        {
            var players = await _context.Players.ToListAsync();
            var normalizedType = type.Trim().ToLower();

            var rankedPlayers = normalizedType switch
            {
                "attack" => players.OrderByDescending(p => p.AttackScore).ThenBy(p => p.Username).ToList(),
                "defense" => players.OrderByDescending(p => p.DefenseScore).ThenBy(p => p.Username).ToList(),
                "war" => players.OrderByDescending(p => p.WarScore).ThenBy(p => p.Username).ToList(),
                _ => players.OrderByDescending(p => p.Score).ThenBy(p => p.Username).ToList()
            };

            var result = rankedPlayers.Select((p, index) => new
            {
                Rank = index + 1,
                p.Id,
                p.Username,
                Score = normalizedType switch
                {
                    "attack" => p.AttackScore,
                    "defense" => p.DefenseScore,
                    "war" => p.WarScore,
                    _ => p.Score
                },
                RankChange = 0
            }).ToList();

            return Ok(result);
        }

        [HttpGet("{id}/public-profile")]
        public async Task<IActionResult> GetPublicProfile(Guid id)
        {
            var player = await _context.Players
                .Include(p => p.Settlements)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (player == null)
                return NotFound("Player not found.");

            return Ok(new
            {
                player.Id,
                player.Username,
                player.Score,
                player.AttackScore,
                player.DefenseScore,
                WarScore = player.WarScore,
                SettlementCount = player.Settlements.Count,
                Settlements = player.Settlements
                    .OrderBy(s => s.Name)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name
                    })
                    .ToList()
            });
        }

        // ════════════════════════════════════════════
        // PRIVATE HELPERS
        // ════════════════════════════════════════════

        private object MapPlayerResponse(Player player, bool includeSettlements, bool wasMigrated = false)
        {
            return new
            {
                player.Id,
                player.Username,
                player.Email,
                player.CreatedAtUtc,
                player.IsActive,
                player.Score,
                player.AttackScore,
                player.DefenseScore,
                WarScore = player.WarScore,
                player.TriumphPoints,
                player.AvailableWarPoints,
                ConquestLevel = player.ConquestLevel,
                MaxSettlements = player.MaxSettlements,
                TriumphPointsForNextLevel = player.TriumphPointsForNextLevel,
                player.DataVersion,
                player.WastelandCoins,
                WasMigrated = wasMigrated,
                Advisors = MapAdvisors(player),
                Settlements = includeSettlements
                ? player.Settlements.Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.PlayerId,
                    s.UsedPopulation,
                    s.PopulationCapacity,
                    s.AvailablePopulation,
                    s.Resources.Water,
                    s.Resources.Food,
                    s.Resources.Scrap,
                    s.Resources.Fuel,
                    s.Resources.Energy,
                    s.Resources.RareTech
                }).ToList()
                : null
            };
        }

        private object MapAdvisors(Player player)
        {
            return new
            {
                Commander = new
                {
                    Active = player.IsCommanderCurrentlyActive(),
                    ExpiresUtc = player.CommanderExpiresUtc
                },
                Quartermaster = new
                {
                    Active = player.IsQuartermasterCurrentlyActive(),
                    ExpiresUtc = player.QuartermasterExpiresUtc
                },
                TechPriest = new
                {
                    Active = player.IsTechPriestCurrentlyActive(),
                    ExpiresUtc = player.TechPriestExpiresUtc
                },
                Warlord = new
                {
                    Active = player.IsWarlordCurrentlyActive(),
                    ExpiresUtc = player.WarlordExpiresUtc
                },
                ScoutMaster = new
                {
                    Active = player.IsScoutMasterCurrentlyActive(),
                    ExpiresUtc = player.ScoutMasterExpiresUtc
                }
            };
        }
    }

    public class CreatePlayerRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SettlementName { get; set; } = string.Empty;
    }

    public class SetRelationRequest
    {
        public string TargetPlayerId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class ActivateAdvisorRequest
    {
        public int DurationDays { get; set; } = 14;
    }

    public class PurchaseCoinsRequest
    {
        public string? PackageId { get; set; }
    }
}