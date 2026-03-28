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
                    : 0,
                NpcUnits = string.IsNullOrEmpty(s.NpcUnitsJson) ? null
                    : JsonSerializer.Deserialize<Dictionary<string, int>>(s.NpcUnitsJson),
                LootItems = string.IsNullOrEmpty(s.LootItemsJson) ? null
                    : JsonSerializer.Deserialize<List<string>>(s.LootItemsJson)
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
                              (o.CompletedAtUtc == null || o.CompletedAtUtc >= DateTime.UtcNow.AddHours(-24))))
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
                        var scoutPoiState = await _db.PoiStates
                            .FirstOrDefaultAsync(p => p.PoiId == op.TargetPoiId);
                        if (scoutPoiState == null)
                        {
                            scoutPoiState = new PoiState(op.TargetPoiId ?? "unknown");
                            _db.PoiStates.Add(scoutPoiState);
                        }

                        if (!scoutPoiState.IsInitialized)
                        {
                            var genNpcUnits = GenerateNpcUnits(op.TargetPoiId ?? "unknown");
                            int genTier = genNpcUnits.Values.Sum() > 20 ? 3 : genNpcUnits.Values.Sum() > 10 ? 2 : 1;
                            var genLootItems = GeneratePoiLoot(op.TargetPoiId ?? "unknown", genTier);
                            scoutPoiState.Initialize(
                                JsonSerializer.Serialize(genNpcUnits),
                                JsonSerializer.Serialize(genLootItems));
                        }

                        var currentNpcUnits = string.IsNullOrEmpty(scoutPoiState.NpcUnitsJson)
                            ? new Dictionary<string, int>()
                            : JsonSerializer.Deserialize<Dictionary<string, int>>(scoutPoiState.NpcUnitsJson)
                              ?? new Dictionary<string, int>();
                        var currentLootItems = string.IsNullOrEmpty(scoutPoiState.LootItemsJson)
                            ? new List<string>()
                            : JsonSerializer.Deserialize<List<string>>(scoutPoiState.LootItemsJson)
                              ?? new List<string>();

                        int tier = currentNpcUnits.Values.Sum() > 20 ? 3 : currentNpcUnits.Values.Sum() > 10 ? 2 : 1;
                        var resultJson = JsonSerializer.Serialize(new
                        {
                            success = true,
                            poiId = op.TargetPoiId,
                            npcUnits = currentNpcUnits,
                            lootItems = currentLootItems,
                            tier
                        });
                        op.SetResult(resultJson);

                        // Send report message to attacker player on arrival
                        if (settlementToPlayer.TryGetValue(op.AttackerSettlementId, out var playerId))
                        {
                            var unitLines = string.Join(", ", currentNpcUnits.Select(u => $"{u.Value}x {u.Key}"));
                            string poiName = !string.IsNullOrEmpty(op.TargetPoiLabel)
                                ? op.TargetPoiLabel
                                : op.TargetPoiId ?? "Unknown POI";
                            _db.Messages.Add(new Message(
                                senderPlayerId: playerId,
                                receiverPlayerId: playerId,
                                subject: $"Scout Report \u2014 {poiName}",
                                body: JsonSerializer.Serialize(new
                                {
                                    isScoutReport = true,
                                    poiName,
                                    tier,
                                    npcUnits = currentNpcUnits,
                                    lootItems = currentLootItems
                                }),
                                messageType: "report"));
                        }
                    }

                    if (op.OperationType == "raid_poi")
                    {
                        var raidPoiState = await _db.PoiStates
                            .FirstOrDefaultAsync(p => p.PoiId == op.TargetPoiId);
                        if (raidPoiState == null)
                        {
                            raidPoiState = new PoiState(op.TargetPoiId ?? "unknown");
                            _db.PoiStates.Add(raidPoiState);
                        }

                        if (!raidPoiState.IsInitialized)
                        {
                            var genNpcUnits = GenerateNpcUnits(op.TargetPoiId ?? "unknown");
                            int genTier = genNpcUnits.Values.Sum() > 20 ? 3 : genNpcUnits.Values.Sum() > 10 ? 2 : 1;
                            var genLootItems = GeneratePoiLoot(op.TargetPoiId ?? "unknown", genTier);
                            raidPoiState.Initialize(
                                JsonSerializer.Serialize(genNpcUnits),
                                JsonSerializer.Serialize(genLootItems));
                        }

                        // Load attacker units
                        var attackerSentUnits = string.IsNullOrEmpty(op.SentUnitsJson)
                            ? new Dictionary<string, int>()
                            : JsonSerializer.Deserialize<Dictionary<string, int>>(op.SentUnitsJson)
                              ?? new Dictionary<string, int>();

                        // Load all unit definitions for combat stats
                        var allUnitDefs = TheFallenWastes_Domain.UnitFactory.UnitFactory.CreateStarterUnits()
                            .ToDictionary(u => u.Name, u => u);

                        // Simple combat: sum attacker total attack vs NPC total defense
                        int attackerTotalAttack = attackerSentUnits
                            .Sum(kvp => (allUnitDefs.TryGetValue(kvp.Key, out var ua) ? ua.AttackPower : 10) * kvp.Value);

                        var raidNpcUnits = string.IsNullOrEmpty(raidPoiState.NpcUnitsJson)
                            ? new Dictionary<string, int>()
                            : JsonSerializer.Deserialize<Dictionary<string, int>>(raidPoiState.NpcUnitsJson)
                              ?? new Dictionary<string, int>();

                        int npcTotalDefense = raidNpcUnits
                            .Sum(kvp => (allUnitDefs.TryGetValue(kvp.Key, out var ud) ? ud.DefenseVsBallistic : 5) * kvp.Value);

                        bool attackerWins = attackerTotalAttack > npcTotalDefense;

                        // Calculate losses (proportional)
                        var attackerLosses = new Dictionary<string, int>();
                        var npcLosses = new Dictionary<string, int>();

                        if (attackerWins)
                        {
                            npcLosses = raidNpcUnits.ToDictionary(k => k.Key, k => k.Value);
                            double lossRatio = npcTotalDefense > 0
                                ? Math.Min(0.5, (double)npcTotalDefense / attackerTotalAttack) : 0;
                            foreach (var kvp in attackerSentUnits)
                            {
                                int lost = (int)Math.Ceiling(kvp.Value * lossRatio);
                                if (lost > 0) attackerLosses[kvp.Key] = lost;
                            }
                        }
                        else
                        {
                            attackerLosses = attackerSentUnits.ToDictionary(k => k.Key, k => k.Value);
                            double npcLossRatio = attackerTotalAttack > 0
                                ? Math.Min(0.7, (double)attackerTotalAttack / npcTotalDefense) : 0;
                            foreach (var kvp in raidNpcUnits)
                            {
                                int lost = (int)Math.Floor(kvp.Value * npcLossRatio);
                                if (lost > 0) npcLosses[kvp.Key] = lost;
                            }
                        }

                        // Apply NPC losses to PoiState
                        if (npcLosses.Any())
                            raidPoiState.ApplyNpcLosses(npcLosses);

                        // Calculate surviving attacker units
                        var survivingAttackerUnits = attackerSentUnits
                            .ToDictionary(k => k.Key, k => k.Value - (attackerLosses.TryGetValue(k.Key, out var l) ? l : 0))
                            .Where(k => k.Value > 0)
                            .ToDictionary(k => k.Key, k => k.Value);

                        var remainingNpcAfterBattle = string.IsNullOrEmpty(raidPoiState.NpcUnitsJson)
                            ? new Dictionary<string, int>()
                            : JsonSerializer.Deserialize<Dictionary<string, int>>(raidPoiState.NpcUnitsJson)
                              ?? new Dictionary<string, int>();

                        var combatResultJson = JsonSerializer.Serialize(new
                        {
                            attackerWins,
                            attackerSentUnits,
                            attackerLosses,
                            attackerSurvived = survivingAttackerUnits,
                            npcLosses,
                            remainingNpcUnits = remainingNpcAfterBattle
                        });
                        op.SetResult(combatResultJson);

                        // Send battle report
                        var raidAttackerSettlement = await _db.Settlements
                            .FirstOrDefaultAsync(s => s.Id == op.AttackerSettlementId);
                        if (raidAttackerSettlement != null)
                        {
                            var raidAttackerPlayer = await _db.Players
                                .FirstOrDefaultAsync(p => p.Id == raidAttackerSettlement.PlayerId);
                            if (raidAttackerPlayer != null)
                            {
                                string attackerLossText = attackerLosses.Any()
                                    ? string.Join(", ", attackerLosses.Select(u => $"{u.Value}x {u.Key}"))
                                    : "none";
                                string npcLossText = npcLosses.Any()
                                    ? string.Join(", ", npcLosses.Select(u => $"{u.Value}x {u.Key}"))
                                    : "none";
                                var lootCollected = attackerWins && raidPoiState.LootItemsJson != null
                                    ? JsonSerializer.Deserialize<List<string>>(raidPoiState.LootItemsJson) ?? new List<string>()
                                    : new List<string>();
                                _db.Messages.Add(new Message(
                                    senderPlayerId: raidAttackerPlayer.Id,
                                    receiverPlayerId: raidAttackerPlayer.Id,
                                    subject: $"Battle Report \u2014 {op.TargetPoiLabel ?? op.TargetPoiId}",
                                    body: JsonSerializer.Serialize(new
                                    {
                                        poiName = op.TargetPoiLabel ?? op.TargetPoiId,
                                        attackerWins,
                                        attackerSentUnits,
                                        attackerLosses,
                                        attackerSurvived = survivingAttackerUnits,
                                        npcLosses,
                                        remainingNpcUnits = remainingNpcAfterBattle,
                                        lootCollected
                                    }),
                                    messageType: "report"));
                            }
                        }
                    }

                    op.MarkArrived();
                    // Scout: immediately start returning so it doesn't clutter movements panel.
                    // Frontend reads resultJson from the 24h window filter.
                    if (op.OperationType == "scout_poi")
                    {
                        int returnSeconds = (int)(op.ArrivesAtUtc - op.StartedAtUtc).TotalSeconds;
                        op.MarkReturning(returnSeconds);
                    }
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
                             (o.CompletedAtUtc == null || o.CompletedAtUtc >= DateTime.UtcNow.AddHours(-24))))
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

                    // EF Core loads DateTime with Kind=Unspecified; force Utc so JSON serializer
                    // includes the 'Z' suffix and JavaScript parses as UTC (not local time).
                    static DateTime Utc(DateTime dt) => DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                    static DateTime? UtcN(DateTime? dt) => dt.HasValue ? DateTime.SpecifyKind(dt.Value, DateTimeKind.Utc) : null;

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
                        StartedAtUtc = Utc(o.StartedAtUtc),
                        ArrivesAtUtc = Utc(o.ArrivesAtUtc),
                        ReturnsAtUtc = UtcN(o.ReturnsAtUtc),
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
                travelSeconds: Math.Max(30, Math.Min(86400, request.TravelSeconds)));

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

        // ─── POI loot generation ────────────────────────────────────────────────────
        // Deterministic: same poiId + tier always produces the same loot.
        private static List<string> GeneratePoiLoot(string poiId, int tier)
        {
            int hash = Math.Abs(poiId.GetHashCode());
            var pool = tier switch
            {
                1 => new[] {
                    "Cracked Circuit Board", "Burned Power Cell",
                    "Scrap Bundle", "Fractured Optics Module"
                },
                2 => new[] {
                    "Damaged Servo Bundle", "Broken Drone Core",
                    "Pre-War Guidance Chip", "Encrypted Datacore"
                },
                _ => new[] {
                    "Ancient Data Core", "Prototype Schematic",
                    "Reactor Fragment", "Vault Artifact"
                }
            };
            int count = 2 + (hash % 3);
            return Enumerable.Range(0, count)
                .Select(i => pool[(hash + i * 7) % pool.Length])
                .Distinct()
                .ToList();
        }

        // ─── Travel time helper
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
