using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheFallenWastes_Domain.Entities;
using TheFallenWastes_Domain.Enums;
using TheFallenWastes_Domain.GameData;
using TheFallenWastes_Infrastructure;

namespace TheFallenWastes_WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OperationsController : ControllerBase
    {
        private readonly GameDbContext _db;

        public OperationsController(GameDbContext db)
        {
            _db = db;
        }

        // ═══════════════════════════════════════════════════════════════════
        // GET /api/Operations/poi-states
        // Returns all POI states; auto-respawns any cleared POIs past their timer
        // ═══════════════════════════════════════════════════════════════════
        [HttpGet("poi-states")]
        public async Task<IActionResult> GetPoiStates()
        {
            var states = await _db.PoiStates.ToListAsync();
            var now = DateTime.UtcNow;

            foreach (var s in states.Where(s => s.ShouldRespawn()))
                s.Respawn();

            await _db.SaveChangesAsync();

            return Ok(states.Select(s => new
            {
                s.PoiId,
                s.IsCleared,
                s.ClearedAtUtc,
                s.NextRespawnUtc,
                RespawnInSeconds = s.IsCleared
                    ? (int)Math.Max(0, (s.NextRespawnUtc - now).TotalSeconds)
                    : 0
            }));
        }

        // ═══════════════════════════════════════════════════════════════════
        // GET /api/Operations/settlement/{settlementId}
        // Returns all active (non-completed) operations for this settlement
        // ═══════════════════════════════════════════════════════════════════
        [HttpGet("settlement/{settlementId}")]
        public async Task<IActionResult> GetOperationsForSettlement(Guid settlementId)
        {
            var operations = await _db.Operations
                .Where(o => (o.Phase != "completed" ||
                             (o.OperationType == "scout_poi" &&
                              o.CompletedAtUtc >= DateTime.UtcNow.AddHours(-24)))
                            && (o.AttackerSettlementId == settlementId || o.TargetSettlementId == settlementId))
                .ToListAsync();

            var now = DateTime.UtcNow;

            // Resolve phase transitions based on current time
            // Collect attacker settlement -> player mapping for report messages
            var attackerSettlementIds = operations
                .Where(o => o.Phase == "outbound" && now >= o.ArrivesAtUtc)
                .Select(o => o.AttackerSettlementId)
                .Distinct()
                .ToList();

            Dictionary<Guid, Guid> settlementToPlayer = new();
            if (attackerSettlementIds.Count > 0)
            {
                settlementToPlayer = await _db.Settlements
                    .Where(s => attackerSettlementIds.Contains(s.Id))
                    .ToDictionaryAsync(s => s.Id, s => s.PlayerId);
            }

            foreach (var op in operations)
            {
                if (op.Phase == "outbound" && now >= op.ArrivesAtUtc)
                {
                    if (op.OperationType == "scout_poi")
                    {
                        var npcUnits = GenerateNpcUnits(op.TargetPoiId ?? "unknown");
                        int tier = npcUnits.Values.Sum() > 20 ? 3 : npcUnits.Values.Sum() > 10 ? 2 : 1;
                        var resultJson = JsonSerializer.Serialize(new
                        {
                            success = true,
                            poiId = op.TargetPoiId,
                            npcUnits,
                            tier
                        });
                        op.SetResult(resultJson);

                        // Send report message to attacker player on arrival
                        if (settlementToPlayer.TryGetValue(op.AttackerSettlementId, out var playerId))
                        {
                            var unitLines = string.Join(", ", npcUnits.Select(u => $"{u.Value}x {u.Key}"));
                            string poiName = !string.IsNullOrEmpty(op.TargetPoiLabel)
                                ? op.TargetPoiLabel
                                : op.TargetPoiId ?? "Unknown POI";
                            _db.Messages.Add(new Message(
                                senderPlayerId: playerId,
                                receiverPlayerId: playerId,
                                subject: $"Scout Report \u2014 {poiName}",
                                body: $"Scout of {poiName} returned. Tier {tier} POI. Defenders: {unitLines}.",
                                messageType: "report"));
                        }
                    }

                    if (op.OperationType == "raid_poi")
                    {
                        var poiState = await _db.PoiStates
                            .FirstOrDefaultAsync(p => p.PoiId == op.TargetPoiId);
                        if (poiState == null)
                        {
                            poiState = new PoiState(op.TargetPoiId ?? "unknown");
                            _db.PoiStates.Add(poiState);
                        }
                        poiState.MarkCleared();
                    }

                    op.MarkArrived();
                    if (op.OperationType == "scout_poi")
                        op.MarkCompleted(op.ResultJson ?? "{}");
                }
                else if (op.Phase == "returning" && op.ReturnsAtUtc.HasValue && now >= op.ReturnsAtUtc.Value)
                    op.MarkCompleted(op.ResultJson ?? "{}");
            }

            await _db.SaveChangesAsync();

            // Collect settlement IDs for name resolution
            var attackerIds = operations.Select(o => o.AttackerSettlementId).Distinct().ToList();
            var targetIds = operations
                .Where(o => o.TargetSettlementId.HasValue)
                .Select(o => o.TargetSettlementId!.Value)
                .Distinct()
                .ToList();

            var allIds = attackerIds.Concat(targetIds).Distinct().ToList();
            var settlementNames = await _db.Settlements
                .Where(s => allIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Name);

            var result = operations
                .Where(o => o.Phase != "completed" ||
                            (o.OperationType == "scout_poi" &&
                             o.CompletedAtUtc >= DateTime.UtcNow.AddHours(-24)))
                .Select(o =>
                {
                    var sentUnits = string.IsNullOrEmpty(o.SentUnitsJson)
                        ? new Dictionary<string, int>()
                        : JsonSerializer.Deserialize<Dictionary<string, int>>(o.SentUnitsJson)
                          ?? new Dictionary<string, int>();

                    string targetName = o.TargetSettlementId.HasValue
                        ? settlementNames.GetValueOrDefault(o.TargetSettlementId.Value, "Unknown")
                        : o.TargetPoiId ?? "Unknown";

                    double remainingSeconds = o.Phase == "outbound"
                        ? Math.Max(0, (o.ArrivesAtUtc - now).TotalSeconds)
                        : o.ReturnsAtUtc.HasValue
                            ? Math.Max(0, (o.ReturnsAtUtc.Value - now).TotalSeconds)
                            : 0;

                    return new
                    {
                        o.Id,
                        o.OperationType,
                        o.Phase,
                        o.AttackerSettlementId,
                        o.TargetSettlementId,
                        o.TargetPoiId,
                        o.TargetPoiLabel,
                        OriginSettlementName = settlementNames.GetValueOrDefault(o.AttackerSettlementId, "Unknown"),
                        TargetName = targetName,
                        SentUnits = sentUnits,
                        o.ScoutRareTech,
                        o.RaidMode,
                        o.StartedAtUtc,
                        o.ArrivesAtUtc,
                        o.ReturnsAtUtc,
                        TravelSeconds = (int)(o.ArrivesAtUtc - o.StartedAtUtc).TotalSeconds,
                        RemainingSeconds = (int)remainingSeconds,
                        IsOwn = o.AttackerSettlementId == settlementId,
                        o.ResultJson,
                        LootItemsEarned = o.OperationType == "raid_poi"
                            ? o.CalculateLootItemsEarned(now) : 0,
                        o.LootIntervalSeconds
                    };
                })
                .ToList();

            return Ok(result);
        }

        // ═══════════════════════════════════════════════════════════════════
        // POST /api/Operations/settlement/{settlementId}/recall/{operationId}
        // ═══════════════════════════════════════════════════════════════════
        [HttpPost("settlement/{settlementId}/recall/{operationId}")]
        public async Task<IActionResult> RecallOperation(Guid settlementId, Guid operationId)
        {
            var operation = await _db.Operations
                .FirstOrDefaultAsync(o => o.Id == operationId
                    && o.AttackerSettlementId == settlementId
                    && o.Phase == "arrived");
            if (operation == null)
                return NotFound("No active arrived operation found.");

            int lootEarned = operation.CalculateLootItemsEarned(DateTime.UtcNow);
            operation.SetLootCollected(lootEarned);
            int returnSeconds = (int)(operation.ArrivesAtUtc - operation.StartedAtUtc).TotalSeconds;
            operation.MarkReturning(returnSeconds);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                lootEarned,
                returnsAtUtc = operation.ReturnsAtUtc,
                message = $"Troops recalled. {lootEarned} loot item(s) collected."
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // POST /api/Operations/settlement/{settlementId}/scout-poi
        // ═══════════════════════════════════════════════════════════════════
        [HttpPost("settlement/{settlementId}/scout-poi")]
        public async Task<IActionResult> ScoutPoi(Guid settlementId, [FromBody] ScoutPoiRequest request)
        {
            var settlement = await _db.Settlements
                .Include(s => s.Buildings)
                .FirstOrDefaultAsync(s => s.Id == settlementId);

            if (settlement == null)
                return NotFound("Settlement not found.");

            // Validate RaidVault level >= 1
            int raidVaultLevel = settlement.GetBuildingLevel(BuildingType.RaidVault);
            if (raidVaultLevel < 1)
                return BadRequest("RaidVault building level 1 required to scout.");

            // Validate RareTech
            if (settlement.VaultRareTech < request.RareTechAmount)
                return BadRequest("Not enough RareTech in Relic Vault.");

            // Deduct from vault
            settlement.WithdrawFromVault(request.RareTechAmount);

            // Create and immediately resolve operation (no background worker yet)
            var sentUnitsJson = JsonSerializer.Serialize(new Dictionary<string, int>());
            var operation = new Operation(
                attackerSettlementId: settlementId,
                targetSettlementId: null,
                targetPoiId: request.TargetPoiId,
                targetPoiLabel: request.TargetPoiLabel,
                operationType: "scout_poi",
                sentUnitsJson: sentUnitsJson,
                scoutRareTech: request.RareTechAmount,
                raidMode: null,
                travelSeconds: Math.Max(30, Math.Min(86400, request.TravelSeconds)));

            // Operation starts as "outbound" — resolved when GET is polled after ArrivesAtUtc

            _db.Operations.Add(operation);

            await _db.SaveChangesAsync();

            return Ok(new { success = true, operationId = operation.Id, message = "Scout dispatched." });
        }

        // ═══════════════════════════════════════════════════════════════════
        // POST /api/Operations/settlement/{settlementId}/scout-settlement
        // ═══════════════════════════════════════════════════════════════════
        [HttpPost("settlement/{settlementId}/scout-settlement")]
        public async Task<IActionResult> ScoutSettlement(Guid settlementId, [FromBody] ScoutSettlementRequest request)
        {
            var attacker = await _db.Settlements
                .Include(s => s.Buildings)
                .FirstOrDefaultAsync(s => s.Id == settlementId);

            if (attacker == null)
                return NotFound("Attacker settlement not found.");

            var defender = await _db.Settlements
                .Include(s => s.Buildings)
                .FirstOrDefaultAsync(s => s.Id == request.TargetSettlementId);

            if (defender == null)
                return NotFound("Target settlement not found.");

            // Validate attacker has enough RareTech in vault
            if (attacker.VaultRareTech < request.RareTechAmount)
                return BadRequest("Not enough RareTech in Relic Vault.");

            // RaidVault mechanic: defender's at-risk amount is their vault stock
            int defenderStock = defender.VaultRareTech;
            bool attackerWins = request.RareTechAmount > defenderStock;
            int stolenAmount = 0;

            // Deduct attacker vault RareTech regardless
            attacker.WithdrawFromVault(request.RareTechAmount);

            var sentUnitsJson = JsonSerializer.Serialize(new Dictionary<string, int>());
            var operation = new Operation(
                attackerSettlementId: settlementId,
                targetSettlementId: request.TargetSettlementId,
                targetPoiId: null,
                targetPoiLabel: null,
                operationType: "scout_settlement",
                sentUnitsJson: sentUnitsJson,
                scoutRareTech: request.RareTechAmount,
                raidMode: null,
                travelSeconds: 60);

            var attackerPlayer = await _db.Players.FirstOrDefaultAsync(p => p.Id == attacker.PlayerId);
            var defenderPlayer = await _db.Players.FirstOrDefaultAsync(p => p.Id == defender.PlayerId);

            string resultJson;

            if (attackerWins)
            {
                stolenAmount = defenderStock;

                // Steal from defender vault
                if (stolenAmount > 0)
                    defender.WithdrawFromVault(stolenAmount);

                resultJson = JsonSerializer.Serialize(new
                {
                    success = true,
                    stolenAmount
                });
                operation.SetResult(resultJson);
                operation.MarkCompleted(resultJson);

                // Report to attacker
                if (attackerPlayer != null)
                {
                    _db.Messages.Add(new Message(
                        senderPlayerId: attackerPlayer.Id,
                        receiverPlayerId: attackerPlayer.Id,
                        subject: "Scout Report — Settlement Raided",
                        body: $"Scout success. Stole {stolenAmount} RareTech.",
                        messageType: "report"));
                }

                // Notification to defender
                if (defenderPlayer != null)
                {
                    _db.Messages.Add(new Message(
                        senderPlayerId: defenderPlayer.Id,
                        receiverPlayerId: defenderPlayer.Id,
                        subject: "Relic Vault Raided!",
                        body: $"Your Relic Vault was raided! Lost {stolenAmount} RareTech.",
                        messageType: "notification"));
                }
            }
            else
            {
                resultJson = JsonSerializer.Serialize(new { success = false, stolenAmount = 0 });
                operation.SetResult(resultJson);
                operation.MarkCompleted(resultJson);

                // Report to attacker only
                if (attackerPlayer != null)
                {
                    _db.Messages.Add(new Message(
                        senderPlayerId: attackerPlayer.Id,
                        receiverPlayerId: attackerPlayer.Id,
                        subject: "Scout Report — Scout Failed",
                        body: "Scout failed. Target vault was stronger.",
                        messageType: "report"));
                }
            }

            _db.Operations.Add(operation);
            await _db.SaveChangesAsync();

            return Ok(new { success = attackerWins, stolenAmount, message = attackerWins ? $"Scout success. Stole {stolenAmount} RareTech." : "Scout failed. Target vault was stronger." });
        }

        // ═══════════════════════════════════════════════════════════════════
        // POST /api/Operations/settlement/{settlementId}/attack-poi
        // ═══════════════════════════════════════════════════════════════════
        [HttpPost("settlement/{settlementId}/attack-poi")]
        public async Task<IActionResult> AttackPoi(Guid settlementId, [FromBody] AttackPoiRequest request)
        {
            var settlement = await _db.Settlements
                .Include(s => s.Buildings)
                .FirstOrDefaultAsync(s => s.Id == settlementId);

            if (settlement == null)
                return NotFound("Settlement not found.");

            // Validate units
            if (request.Units == null || request.Units.Count == 0)
                return BadRequest("No units specified.");

            foreach (var kvp in request.Units)
            {
                settlement.UnitInventory.TryGetValue(kvp.Key, out int available);
                if (available < kvp.Value)
                    return BadRequest($"Not enough {kvp.Key} units. Available: {available}, requested: {kvp.Value}.");
            }

            // Deduct units
            foreach (var kvp in request.Units)
            {
                settlement.UnitInventory[kvp.Key] -= kvp.Value;
                if (settlement.UnitInventory[kvp.Key] == 0)
                    settlement.UnitInventory.Remove(kvp.Key);
            }

            int travelSeconds = Math.Max(30, Math.Min(86400, request.TravelSeconds));

            var sentUnitsJson = JsonSerializer.Serialize(request.Units);
            var operation = new Operation(
                attackerSettlementId: settlementId,
                targetSettlementId: null,
                targetPoiId: request.TargetPoiId,
                targetPoiLabel: null,
                operationType: "raid_poi",
                sentUnitsJson: sentUnitsJson,
                scoutRareTech: null,
                raidMode: request.RaidMode,
                travelSeconds: travelSeconds);

            _db.Operations.Add(operation);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                operationId = operation.Id,
                phase = operation.Phase,
                arrivesAtUtc = operation.ArrivesAtUtc,
                message = "Troops dispatched."
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // POST /api/Operations/settlement/{settlementId}/attack-settlement
        // ═══════════════════════════════════════════════════════════════════
        [HttpPost("settlement/{settlementId}/attack-settlement")]
        public async Task<IActionResult> AttackSettlement(Guid settlementId, [FromBody] AttackSettlementRequest request)
        {
            var settlement = await _db.Settlements
                .Include(s => s.Buildings)
                .FirstOrDefaultAsync(s => s.Id == settlementId);

            if (settlement == null)
                return NotFound("Settlement not found.");

            if (request.Units == null || request.Units.Count == 0)
                return BadRequest("No units specified.");

            foreach (var kvp in request.Units)
            {
                settlement.UnitInventory.TryGetValue(kvp.Key, out int available);
                if (available < kvp.Value)
                    return BadRequest($"Not enough {kvp.Key} units. Available: {available}, requested: {kvp.Value}.");
            }

            // Deduct units
            foreach (var kvp in request.Units)
            {
                settlement.UnitInventory[kvp.Key] -= kvp.Value;
                if (settlement.UnitInventory[kvp.Key] == 0)
                    settlement.UnitInventory.Remove(kvp.Key);
            }

            var sentUnitsJson = JsonSerializer.Serialize(request.Units);
            var operation = new Operation(
                attackerSettlementId: settlementId,
                targetSettlementId: request.TargetSettlementId,
                targetPoiId: null,
                targetPoiLabel: null,
                operationType: "attack_settlement",
                sentUnitsJson: sentUnitsJson,
                scoutRareTech: null,
                raidMode: null,
                travelSeconds: Math.Max(30, Math.Min(86400, request.TravelSeconds)));

            _db.Operations.Add(operation);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                operationId = operation.Id,
                phase = operation.Phase,
                arrivesAtUtc = operation.ArrivesAtUtc
            });
        }

        // ═══════════════════════════════════════════════════════════════════
        // POST /api/Operations/settlement/{settlementId}/reinforce-poi
        // ═══════════════════════════════════════════════════════════════════
        [HttpPost("settlement/{settlementId}/reinforce-poi")]
        public async Task<IActionResult> ReinforcePoi(Guid settlementId, [FromBody] ReinforcePoiRequest request)
        {
            var settlement = await _db.Settlements
                .Include(s => s.Buildings)
                .FirstOrDefaultAsync(s => s.Id == settlementId);

            if (settlement == null)
                return NotFound("Settlement not found.");

            if (request.Units == null || request.Units.Count == 0)
                return BadRequest("No units specified.");

            foreach (var kvp in request.Units)
            {
                settlement.UnitInventory.TryGetValue(kvp.Key, out int available);
                if (available < kvp.Value)
                    return BadRequest($"Not enough {kvp.Key} units. Available: {available}, requested: {kvp.Value}.");
            }

            // Deduct units
            foreach (var kvp in request.Units)
            {
                settlement.UnitInventory[kvp.Key] -= kvp.Value;
                if (settlement.UnitInventory[kvp.Key] == 0)
                    settlement.UnitInventory.Remove(kvp.Key);
            }

            var sentUnitsJson = JsonSerializer.Serialize(request.Units);
            var operation = new Operation(
                attackerSettlementId: settlementId,
                targetSettlementId: null,
                targetPoiId: request.TargetPoiId,
                targetPoiLabel: null,
                operationType: "reinforce_poi",
                sentUnitsJson: sentUnitsJson,
                scoutRareTech: null,
                raidMode: null,
                travelSeconds: Math.Max(30, Math.Min(86400, request.TravelSeconds)));

            _db.Operations.Add(operation);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                operationId = operation.Id,
                phase = operation.Phase,
                arrivesAtUtc = operation.ArrivesAtUtc
            });
        }

        // ─── Request DTOs

        public class ScoutPoiRequest
        {
            public string TargetPoiId { get; set; } = string.Empty;
            public int RareTechAmount { get; set; }
            public string TargetPoiLabel { get; set; } = string.Empty;
            public int TravelSeconds { get; set; } = 60;
        }

        public class ScoutSettlementRequest
        {
            public Guid TargetSettlementId { get; set; }
            public int RareTechAmount { get; set; }
            public int TravelSeconds { get; set; } = 120;
        }

        public class AttackPoiRequest
        {
            public string TargetPoiId { get; set; } = string.Empty;
            public Dictionary<string, int> Units { get; set; } = new();
            public string RaidMode { get; set; } = "quick";
            public int TravelSeconds { get; set; } = 300;
        }

        public class AttackSettlementRequest
        {
            public Guid TargetSettlementId { get; set; }
            public Dictionary<string, int> Units { get; set; } = new();
            public int TravelSeconds { get; set; } = 120;
        }

        public class ReinforcePoiRequest
        {
            public string TargetPoiId { get; set; } = string.Empty;
            public Dictionary<string, int> Units { get; set; } = new();
            public int TravelSeconds { get; set; } = 120;
        }

        // ─── NPC unit generation ──────────────────────────────────────────────
        // Deterministic: same poiId always produces the same defenders.
        private static Dictionary<string, int> GenerateNpcUnits(string poiId, string? poiType = null)
        {
            int hash = Math.Abs(poiId.GetHashCode());
            var units = new Dictionary<string, int>();
            int tier = (hash % 3) + 1;

            if (tier == 1)
            {
                units["Scavenger"] = 5 + (hash % 10);
                units["Raider"] = 2 + (hash % 5);
            }
            else if (tier == 2)
            {
                units["Raider"] = 8 + (hash % 8);
                units["Rifleman"] = 3 + (hash % 5);
                units["Outpost Defender"] = 2 + (hash % 3);
            }
            else
            {
                units["Rifleman"] = 10 + (hash % 10);
                units["Shock Fighter"] = 5 + (hash % 5);
                units["Outpost Defender"] = 4 + (hash % 4);
                units["Sniper"] = 2 + (hash % 3);
            }

            return units;
        }

        // ─── Travel time helper ──────────────────────────────────────────────
        // TODO: wire originX/Y and destX/Y from settlement coordinates once added.
        // Formula: distance = sqrt((dx)^2 + (dy)^2); seconds = (int)(distance * 0.3);
        // World is 5400x4200. At distance 500 units → ~150s base. Clamped 30s–24h.
        private static int CalculateTravelSeconds(int originX, int originY, int destX, int destY, int baseSpeedSeconds = 30)
        {
            double distance = Math.Sqrt(Math.Pow(destX - originX, 2) + Math.Pow(destY - originY, 2));
            int seconds = (int)(distance * 0.3);
            return Math.Max(30, Math.Min(seconds, 86400));
        }
    }
}
