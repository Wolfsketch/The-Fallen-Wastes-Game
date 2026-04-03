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
        public async Task<IActionResult> GetPoiStates([FromQuery] Guid? settlementId = null)
        {
            var states = await _db.PoiStates.ToListAsync();
            var now = DateTime.UtcNow;

            // Heal stale data first: un-clear any POI that has loot remaining but IsCleared=true.
            // This must run BEFORE ShouldRespawn so a wrongly-cleared POI with loot isn't wiped.
            foreach (var s in states.Where(s => s.IsCleared && !string.IsNullOrEmpty(s.LootItemsJson)))
            {
                var remainingLoot = JsonSerializer.Deserialize<List<string>>(s.LootItemsJson!) ?? new();
                if (remainingLoot.Count > 0)
                    s.UnClear();
            }

            foreach (var s in states.Where(s => s.ShouldRespawn()))
                s.Respawn();

            await _db.SaveChangesAsync();

            // Only expose live LootItems for POIs where THIS settlement has arrived or returning raid troops
            var ownArrivedPoiIds = new HashSet<string>();
            if (settlementId.HasValue)
            {
                var ids = await _db.Operations
                    .Where(o => o.AttackerSettlementId == settlementId.Value
                             && (o.Phase == "arrived" || o.Phase == "returning")
                             && o.OperationType == "raid_poi"
                             && o.TargetPoiId != null)
                    .Select(o => o.TargetPoiId!)
                    .ToListAsync();
                ownArrivedPoiIds = new HashSet<string>(ids);
            }

            return Ok(states.Select(s => new
            {
                s.PoiId,
                s.IsCleared,
                s.ClearedAtUtc,
                s.NextRespawnUtc,
                s.GenerationSeed,
                s.IsRelocating,
                RelocatingAtUtc = s.RelocatingAtUtc.HasValue
                    ? DateTime.SpecifyKind(s.RelocatingAtUtc.Value, DateTimeKind.Utc)
                    : (DateTime?)null,
                RespawnInSeconds = s.IsCleared
                    ? (int)Math.Max(0, (s.NextRespawnUtc - now).TotalSeconds)
                    : 0,
                NpcUnits = string.IsNullOrEmpty(s.NpcUnitsJson) ? null
                    : JsonSerializer.Deserialize<Dictionary<string, int>>(s.NpcUnitsJson),
                LootItems = ownArrivedPoiIds.Contains(s.PoiId) && !string.IsNullOrEmpty(s.LootItemsJson)
                    ? JsonSerializer.Deserialize<List<string>>(s.LootItemsJson)
                    : null
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
                bool siegeStarted = false; // set when a Convoy attack starts a siege (skip auto-return)

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
                                int attackPointsGainedPoi = attackerWins ? npcLosses.Values.Sum() : 0;
                                _db.Messages.Add(new Message(
                                    senderPlayerId: raidAttackerPlayer.Id,
                                    receiverPlayerId: raidAttackerPlayer.Id,
                                    subject: attackerWins
                                        ? $"\u2694 Victory \u2014 {op.TargetPoiLabel ?? op.TargetPoiId}"
                                        : $"\u2717 Defeat \u2014 {op.TargetPoiLabel ?? op.TargetPoiId}",
                                    body: JsonSerializer.Serialize(new
                                    {
                                        isPoiBattleReport = true,
                                        poiName = op.TargetPoiLabel ?? op.TargetPoiId,
                                        attackerWins,
                                        attackerSentUnits,
                                        attackerLosses,
                                        attackerSurvived = survivingAttackerUnits,
                                        npcLosses,
                                        remainingNpcUnits = remainingNpcAfterBattle,
                                        raidMode = op.RaidMode,
                                        lootPendingReturn = attackerWins,
                                        attackPointsGained = attackPointsGainedPoi
                                    }),
                                    messageType: "report"));

                                // ── Score: POI raid attack score + TP ──
                                if (attackerWins)
                                {
                                    int killed = npcLosses.Values.Sum();
                                    if (killed > 0)
                                    {
                                        raidAttackerPlayer.SetAttackScore(raidAttackerPlayer.AttackScore + killed);
                                        raidAttackerPlayer.EarnWarPoints(killed);
                                        _db.Entry(raidAttackerPlayer).Property(p => p.AttackScore).IsModified = true;
                                        _db.Entry(raidAttackerPlayer).Property(p => p.AvailableWarPoints).IsModified = true;
                                    }
                                }
                            }
                        }
                    }

                    // ── Settlement reinforce: troops arrive and stay ─────────────────
                    if (op.OperationType == "reinforce_settlement" && op.TargetSettlementId.HasValue)
                    {
                        // No combat — reinforcement arrives silently and stays until recalled.
                        // Notify the target settlement owner.
                        var targetSettForNotif = await _db.Settlements
                            .FirstOrDefaultAsync(s => s.Id == op.TargetSettlementId.Value);
                        var senderSettForNotif = await _db.Settlements
                            .FirstOrDefaultAsync(s => s.Id == op.AttackerSettlementId);
                        if (targetSettForNotif != null && senderSettForNotif != null)
                        {
                            var targetPlayerNotif = await _db.Players
                                .FirstOrDefaultAsync(p => p.Id == targetSettForNotif.PlayerId);
                            if (targetPlayerNotif != null)
                            {
                                var sentForNotif = string.IsNullOrEmpty(op.SentUnitsJson)
                                    ? new Dictionary<string, int>()
                                    : JsonSerializer.Deserialize<Dictionary<string, int>>(op.SentUnitsJson) ?? new();
                                string unitListNotif = string.Join(", ", sentForNotif.Select(u => $"{u.Value}x {u.Key}"));
                                _db.Messages.Add(new Message(
                                    senderPlayerId: targetPlayerNotif.Id,
                                    receiverPlayerId: targetPlayerNotif.Id,
                                    subject: $"\u26d1 Reinforcement arrived from {senderSettForNotif.Name}",
                                    body: $"Reinforcement has arrived at {targetSettForNotif.Name} from {senderSettForNotif.Name}: {unitListNotif}.",
                                    messageType: "notification"));
                            }
                        }
                        // ResultJson = all units stationed (for recall restoration later)
                        op.SetResult(JsonSerializer.Serialize(new
                        {
                            isReinforceReport = true,
                            stationedUnits = string.IsNullOrEmpty(op.SentUnitsJson)
                                ? new Dictionary<string, int>()
                                : JsonSerializer.Deserialize<Dictionary<string, int>>(op.SentUnitsJson) ?? new()
                        }));
                    }

                    // ── Settlement attack battle resolution ──────────────────
                    if (op.OperationType == "attack_settlement" && op.ResultJson == null && op.TargetSettlementId.HasValue)
                    {
                        var atkSett = await _db.Settlements
                            .Include(s => s.Buildings)
                            .FirstOrDefaultAsync(s => s.Id == op.AttackerSettlementId);
                        var defSett = await _db.Settlements
                            .Include(s => s.Buildings)
                            .FirstOrDefaultAsync(s => s.Id == op.TargetSettlementId.Value);

                        // Reload from DB to bypass EF identity map — ensures fresh UnitInventory
                        // when multiple attacks target the same settlement in one processing tick.
                        if (defSett != null)
                            await _db.Entry(defSett).ReloadAsync();

                        if (atkSett != null && defSett != null)
                        {
                            // ── Check if target is already under siege (fight garrison instead) ──
                            var activeSiegeOnTarget = await _db.Sieges
                                .FirstOrDefaultAsync(s => s.SettlementId == defSett.Id && s.Status == "Active");

                            if (activeSiegeOnTarget != null)
                            {
                                // Counter-attack against the occupying garrison
                                var unitDefsSiege = TheFallenWastes_Domain.UnitFactory.UnitFactory.CreateStarterUnits()
                                    .ToDictionary(u => u.Name, u => u, StringComparer.OrdinalIgnoreCase);
                                var sentSiege = string.IsNullOrEmpty(op.SentUnitsJson)
                                    ? new Dictionary<string, int>()
                                    : JsonSerializer.Deserialize<Dictionary<string, int>>(op.SentUnitsJson) ?? new();
                                var garrison = string.IsNullOrEmpty(activeSiegeOnTarget.GarrisonUnitsJson)
                                    ? new Dictionary<string, int>()
                                    : JsonSerializer.Deserialize<Dictionary<string, int>>(activeSiegeOnTarget.GarrisonUnitsJson) ?? new();

                                int atkPowerSiege = sentSiege.Sum(kvp => (unitDefsSiege.TryGetValue(kvp.Key, out var ua) ? ua.AttackPower : 10) * kvp.Value);
                                int defPowerSiege = garrison.Sum(kvp => (unitDefsSiege.TryGetValue(kvp.Key, out var ud) ? ud.DefenseVsBallistic : 5) * kvp.Value);
                                bool atkWinsSiege = atkPowerSiege > defPowerSiege;

                                var atkLossesS = new Dictionary<string, int>();
                                var garrisonLosses = new Dictionary<string, int>();
                                if (atkWinsSiege)
                                {
                                    garrisonLosses = garrison.ToDictionary(k => k.Key, k => k.Value);
                                    double lr = defPowerSiege > 0 ? Math.Min(0.85, (double)defPowerSiege / atkPowerSiege) : 0.0;
                                    foreach (var kvp in sentSiege) { int l = (int)Math.Ceiling(kvp.Value * lr); if (l > 0) atkLossesS[kvp.Key] = Math.Min(l, kvp.Value); }
                                }
                                else
                                {
                                    atkLossesS = sentSiege.ToDictionary(k => k.Key, k => k.Value);
                                    double dlr = atkPowerSiege > 0 ? Math.Min(0.50, (double)atkPowerSiege / defPowerSiege) : 0.0;
                                    foreach (var kvp in garrison) { int l = (int)Math.Floor(kvp.Value * dlr); if (l > 0) garrisonLosses[kvp.Key] = l; }
                                }
                                var atkSurvivedS = sentSiege.ToDictionary(k => k.Key, k => k.Value - (atkLossesS.TryGetValue(k.Key, out var al) ? al : 0)).Where(k => k.Value > 0).ToDictionary(k => k.Key, k => k.Value);
                                var garrisonSurvived = garrison.ToDictionary(k => k.Key, k => k.Value - (garrisonLosses.TryGetValue(k.Key, out var gl) ? gl : 0)).Where(k => k.Value > 0).ToDictionary(k => k.Key, k => k.Value);

                                bool convoyDestroyed = atkWinsSiege && (!garrisonSurvived.ContainsKey("Convoy") || garrisonSurvived["Convoy"] == 0);
                                if (convoyDestroyed)
                                    activeSiegeOnTarget.ResolveFailed();
                                else
                                    activeSiegeOnTarget.UpdateGarrison(JsonSerializer.Serialize(garrisonSurvived));

                                var siegeCounterJson = JsonSerializer.Serialize(new
                                {
                                    isSettlementBattleReport = true,
                                    isSiegeCounterAttack = true,
                                    attackerWins = atkWinsSiege,
                                    attackerSentUnits = sentSiege,
                                    attackerLosses = atkLossesS,
                                    attackerSurvived = atkSurvivedS,
                                    defenderUnits = garrison,
                                    defenderLosses = garrisonLosses,
                                    defenderSurvived = garrisonSurvived,
                                    siegeFailed = convoyDestroyed,
                                    attackerSettlementName = atkSett.Name,
                                    defenderSettlementName = defSett.Name,
                                    attackPointsGained = atkWinsSiege ? garrisonLosses.Values.Sum() : 0,
                                    defensePointsGained = !atkWinsSiege ? atkLossesS.Values.Sum() : 0,
                                });
                                op.SetResult(siegeCounterJson);

                                var atkPlayerS = await _db.Players.FirstOrDefaultAsync(p => p.Id == atkSett.PlayerId);
                                if (atkPlayerS != null)
                                    _db.Messages.Add(new Message(atkPlayerS.Id, atkPlayerS.Id,
                                        atkWinsSiege ? (convoyDestroyed ? "\u2694 Siege Broken!" : "\u2694 Garrison Damaged") : "\u2717 Garrison Held",
                                        siegeCounterJson, "report"));

                                if (convoyDestroyed)
                                {
                                    // Notify siege attacker that their siege failed
                                    var siegeAttacker = await _db.Players.FirstOrDefaultAsync(p => p.Id == activeSiegeOnTarget.AttackerPlayerId);
                                    if (siegeAttacker != null)
                                        _db.Messages.Add(new Message(siegeAttacker.Id, siegeAttacker.Id,
                                            $"\u26a0 Siege Failed \u2014 {defSett.Name}",
                                            $"Your Convoy was destroyed! The siege of {defSett.Name} has failed.",
                                            "notification"));
                                }
                            }
                            else
                            {
                                // ── Normal battle (no active siege on target) ────────────
                                var unitDefs = TheFallenWastes_Domain.UnitFactory.UnitFactory.CreateStarterUnits()
                                    .ToDictionary(u => u.Name, u => u, StringComparer.OrdinalIgnoreCase);

                                var sentUnitsAtk = string.IsNullOrEmpty(op.SentUnitsJson)
                                    ? new Dictionary<string, int>()
                                    : JsonSerializer.Deserialize<Dictionary<string, int>>(op.SentUnitsJson)
                                      ?? new Dictionary<string, int>();

                                var defUnits = new Dictionary<string, int>(
                                    defSett.UnitInventory, StringComparer.OrdinalIgnoreCase);

                                int atkPower = sentUnitsAtk.Sum(kvp =>
                                    (unitDefs.TryGetValue(kvp.Key, out var ua) ? ua.AttackPower : 10) * kvp.Value);
                                int defPower = defUnits.Sum(kvp =>
                                    (unitDefs.TryGetValue(kvp.Key, out var ud) ? ud.DefenseVsBallistic : 5) * kvp.Value);

                                bool atkWins = atkPower > defPower;

                                var atkLosses = new Dictionary<string, int>();
                                var defLosses = new Dictionary<string, int>();

                                if (atkWins)
                                {
                                    // All defenders eliminated; attacker takes proportional losses
                                    defLosses = defUnits.ToDictionary(k => k.Key, k => k.Value);
                                    double lossRatio = defPower > 0
                                        ? Math.Min(0.85, (double)defPower / atkPower) : 0.0;
                                    foreach (var kvp in sentUnitsAtk)
                                    {
                                        int lost = (int)Math.Ceiling(kvp.Value * lossRatio);
                                        if (lost > 0) atkLosses[kvp.Key] = Math.Min(lost, kvp.Value);
                                    }
                                }
                                else
                                {
                                    // All attackers die on failed assault; defenders take some damage
                                    atkLosses = sentUnitsAtk.ToDictionary(k => k.Key, k => k.Value);
                                    double defLossRatio = atkPower > 0
                                        ? Math.Min(0.50, (double)atkPower / defPower) : 0.0;
                                    foreach (var kvp in defUnits)
                                    {
                                        int lost = (int)Math.Floor(kvp.Value * defLossRatio);
                                        if (lost > 0) defLosses[kvp.Key] = lost;
                                    }
                                }

                                // Apply defender unit losses
                                foreach (var kvp in defLosses)
                                {
                                    if (defSett.UnitInventory.TryGetValue(kvp.Key, out int cur))
                                    {
                                        int remaining = cur - kvp.Value;
                                        if (remaining <= 0) defSett.UnitInventory.Remove(kvp.Key);
                                        else defSett.UnitInventory[kvp.Key] = remaining;
                                    }
                                }
                                _db.Entry(defSett).Property(s => s.UnitInventory).IsModified = true;

                                var atkSurvived = sentUnitsAtk
                                    .ToDictionary(k => k.Key,
                                        k => k.Value - (atkLosses.TryGetValue(k.Key, out var al) ? al : 0))
                                    .Where(k => k.Value > 0)
                                    .ToDictionary(k => k.Key, k => k.Value);

                                var defSurvived = defUnits
                                    .ToDictionary(k => k.Key,
                                        k => k.Value - (defLosses.TryGetValue(k.Key, out var dl) ? dl : 0))
                                    .Where(k => k.Value > 0)
                                    .ToDictionary(k => k.Key, k => k.Value);

                                // Loot: surviving attacker carry capacity, up to 33% of each resource
                                int carryTotal = atkSurvived.Sum(kvp =>
                                    (unitDefs.TryGetValue(kvp.Key, out var uc) ? uc.CarryCapacity : 10) * kvp.Value);

                                var looted = new Dictionary<string, int>
                                    { ["water"]=0, ["food"]=0, ["scrap"]=0, ["fuel"]=0, ["energy"]=0 };

                                if (atkWins && carryTotal > 0)
                                {
                                    var resPool = new (string name, int avail)[]
                                    {
                                        ("water",  defSett.Resources.Water),
                                        ("food",   defSett.Resources.Food),
                                        ("scrap",  defSett.Resources.Scrap),
                                        ("fuel",   defSett.Resources.Fuel),
                                        ("energy", defSett.Resources.Energy),
                                    };
                                    int carryLeft = carryTotal;
                                    foreach (var (rn, ra) in resPool)
                                    {
                                        if (carryLeft <= 0) break;
                                        int maxTake = (int)(ra * 0.33);
                                        int take = Math.Min(maxTake, carryLeft);
                                        if (take > 0) { looted[rn] = take; carryLeft -= take; }
                                    }
                                    defSett.Resources.Spend(
                                        water:  looted["water"],
                                        food:   looted["food"],
                                        scrap:  looted["scrap"],
                                        fuel:   looted["fuel"],
                                        energy: looted["energy"]);
                                }

                                // ── Convoy siege check ───────────────────────────────────────
                                bool sentConvoy = sentUnitsAtk.ContainsKey("Convoy") && sentUnitsAtk["Convoy"] > 0;
                                bool convoyInSurvivors = atkSurvived.ContainsKey("Convoy") && atkSurvived["Convoy"] > 0;

                                // Load atkPlayer and defPlayer first (needed for siege and reports)
                                var atkPlayer = await _db.Players
                                    .Include(p => p.Settlements)
                                    .FirstOrDefaultAsync(p => p.Id == atkSett.PlayerId);
                                var defPlayer = await _db.Players
                                    .FirstOrDefaultAsync(p => p.Id == defSett.PlayerId);

                                bool hasEnoughTp = atkPlayer != null &&
                                    (atkPlayer.Settlements.Count < atkPlayer.MaxSettlements ||
                                     atkPlayer.TriumphPoints >= atkPlayer.TriumphPointsForNextLevel);

                                if (sentConvoy && atkWins && convoyInSurvivors && hasEnoughTp && defPlayer != null)
                                {
                                    // ── START SIEGE ─────────────────────────────────────────
                                    siegeStarted = true;

                                    // Remove Convoy from loot (it sits in the settlement, can't carry)
                                    var garrisonForSiege = new Dictionary<string, int>(atkSurvived);
                                    var newSiege = new TheFallenWastes_Domain.Entities.Siege(
                                        settlementId: defSett.Id,
                                        attackerPlayerId: atkPlayer!.Id,
                                        defenderPlayerId: defPlayer.Id,
                                        convoyOperationId: op.Id,
                                        garrisonUnitsJson: JsonSerializer.Serialize(garrisonForSiege));
                                    _db.Sieges.Add(newSiege);

                                    var battleJsonSiege = JsonSerializer.Serialize(new
                                    {
                                        isSettlementBattleReport = true,
                                        isConvoyAttackReport = true,
                                        siegeStarted = true,
                                        siegeEndsAtUtc = DateTime.SpecifyKind(newSiege.EndsAtUtc, DateTimeKind.Utc),
                                        attackerWins = atkWins,
                                        attackerSentUnits = sentUnitsAtk,
                                        attackerLosses = atkLosses,
                                        attackerSurvived = atkSurvived,
                                        defenderUnits = defUnits,
                                        defenderLosses = defLosses,
                                        defenderSurvived = defSurvived,
                                        lootedResources = looted,
                                        attackerSettlementName = atkSett.Name,
                                        defenderSettlementName = defSett.Name,
                                        attackPointsGained = defLosses.Values.Sum(),
                                        defensePointsGained = 0,
                                    });
                                    op.SetResult(battleJsonSiege);

                                    if (atkPlayer != null)
                                    {
                                        _db.Messages.Add(new Message(
                                            senderPlayerId: atkPlayer.Id,
                                            receiverPlayerId: atkPlayer.Id,
                                            subject: $"\u2694 Siege Started \u2014 {defSett.Name}",
                                            body: battleJsonSiege,
                                            messageType: "report"));
                                    }
                                    if (defPlayer != null)
                                    {
                                        _db.Messages.Add(new Message(
                                            senderPlayerId: defPlayer.Id,
                                            receiverPlayerId: defPlayer.Id,
                                            subject: $"\u26a0 YOUR SETTLEMENT IS UNDER SIEGE \u2014 {defSett.Name}",
                                            body: battleJsonSiege,
                                            messageType: "notification"));
                                    }

                                    // Score: attack points for kills
                                    if (atkPlayer != null)
                                    {
                                        int atkKilledSiege = defLosses.Values.Sum();
                                        if (atkKilledSiege > 0)
                                        {
                                            atkPlayer.SetAttackScore(atkPlayer.AttackScore + atkKilledSiege);
                                            atkPlayer.EarnWarPoints(atkKilledSiege);
                                            _db.Entry(atkPlayer).Property(p => p.AttackScore).IsModified = true;
                                            _db.Entry(atkPlayer).Property(p => p.AvailableWarPoints).IsModified = true;
                                        }
                                    }
                                }
                                else if (sentConvoy && atkWins && convoyInSurvivors && !hasEnoughTp)
                                {
                                    // ── Fake Convoy: won but not enough TP — Convoy returns ──
                                    var battleJsonFake = JsonSerializer.Serialize(new
                                    {
                                        isSettlementBattleReport = true,
                                        isConvoyAttackReport = true,
                                        siegeStarted = false,
                                        convoyReturnedInsuffientTp = true,
                                        attackerWins = atkWins,
                                        attackerSentUnits = sentUnitsAtk,
                                        attackerLosses = atkLosses,
                                        attackerSurvived = atkSurvived,
                                        defenderUnits = defUnits,
                                        defenderLosses = defLosses,
                                        defenderSurvived = defSurvived,
                                        lootedResources = looted,
                                        attackerSettlementName = atkSett.Name,
                                        defenderSettlementName = defSett.Name,
                                        attackPointsGained = defLosses.Values.Sum(),
                                        defensePointsGained = 0,
                                    });
                                    op.SetResult(battleJsonFake);

                                    if (atkPlayer != null)
                                        _db.Messages.Add(new Message(atkPlayer.Id, atkPlayer.Id,
                                            $"\u2694 Victory \u2014 {defSett.Name} (Convoy returned — insufficient TP)",
                                            battleJsonFake, "report"));
                                    if (defPlayer != null)
                                        _db.Messages.Add(new Message(defPlayer.Id, defPlayer.Id,
                                            $"\u2713 Attack repelled from {atkSett.Name}",
                                            battleJsonFake, "report"));

                                    if (atkPlayer != null)
                                    {
                                        int k = defLosses.Values.Sum();
                                        if (k > 0) { atkPlayer.SetAttackScore(atkPlayer.AttackScore + k); atkPlayer.EarnWarPoints(k); _db.Entry(atkPlayer).Property(p => p.AttackScore).IsModified = true; _db.Entry(atkPlayer).Property(p => p.AvailableWarPoints).IsModified = true; }
                                    }
                                    if (defPlayer != null)
                                    {
                                        int kd = atkLosses.Values.Sum();
                                        if (!atkWins && kd > 0) { defPlayer.SetDefenseScore(defPlayer.DefenseScore + kd); defPlayer.EarnWarPoints(kd); _db.Entry(defPlayer).Property(p => p.DefenseScore).IsModified = true; _db.Entry(defPlayer).Property(p => p.AvailableWarPoints).IsModified = true; }
                                    }
                                }
                                else
                                {
                                    // ── Normal attack (no Convoy, or Convoy died/lost) ───────
                                    var battleJson = JsonSerializer.Serialize(new
                                    {
                                        isSettlementBattleReport = true,
                                        attackerWins = atkWins,
                                        attackerSentUnits = sentUnitsAtk,
                                        attackerLosses = atkLosses,
                                        attackerSurvived = atkSurvived,
                                        defenderUnits = defUnits,
                                        defenderLosses = defLosses,
                                        defenderSurvived = defSurvived,
                                        lootedResources = looted,
                                        attackerSettlementName = atkSett.Name,
                                        defenderSettlementName = defSett.Name,
                                        attackPointsGained  = atkWins  ? defLosses.Values.Sum() : 0,
                                        defensePointsGained = !atkWins ? atkLosses.Values.Sum() : 0,
                                    });
                                    op.SetResult(battleJson);

                                    if (atkPlayer != null)
                                    {
                                        _db.Messages.Add(new Message(
                                            senderPlayerId: atkPlayer.Id,
                                            receiverPlayerId: atkPlayer.Id,
                                            subject: atkWins
                                                ? $"\u2694 Victory \u2014 {defSett.Name}"
                                                : $"\u2717 Defeat \u2014 {defSett.Name}",
                                            body: battleJson,
                                            messageType: "report"));
                                    }
                                    if (defPlayer != null && (atkPlayer == null || defPlayer.Id != atkPlayer.Id))
                                    {
                                        var defBodyJson = JsonSerializer.Serialize(new
                                        {
                                            isSettlementBattleReport = true,
                                            isDefenseReport = true,
                                            attackerWins = atkWins,
                                            attackerSentUnits = sentUnitsAtk,
                                            attackerLosses = atkLosses,
                                            attackerSurvived = atkSurvived,
                                            defenderUnits = defUnits,
                                            defenderLosses = defLosses,
                                            defenderSurvived = defSurvived,
                                            lootedResources = looted,
                                            attackerSettlementName = atkSett.Name,
                                            defenderSettlementName = defSett.Name,
                                            attackPointsGained  = atkWins  ? defLosses.Values.Sum() : 0,
                                            defensePointsGained = !atkWins ? atkLosses.Values.Sum() : 0,
                                        });
                                        _db.Messages.Add(new Message(
                                            senderPlayerId: defPlayer.Id,
                                            receiverPlayerId: defPlayer.Id,
                                            subject: atkWins
                                                ? $"\u26a0 Your settlement was raided by {atkSett.Name}"
                                                : $"\u2713 Attack repelled from {atkSett.Name}",
                                            body: defBodyJson,
                                            messageType: "report"));
                                    }

                                    // ── Score: settlement battle attack/defense + TP ──
                                    if (atkPlayer != null)
                                    {
                                        int atkKilled = defLosses.Values.Sum();
                                        if (atkWins && atkKilled > 0)
                                        {
                                            atkPlayer.SetAttackScore(atkPlayer.AttackScore + atkKilled);
                                            atkPlayer.EarnWarPoints(atkKilled);
                                            _db.Entry(atkPlayer).Property(p => p.AttackScore).IsModified = true;
                                            _db.Entry(atkPlayer).Property(p => p.AvailableWarPoints).IsModified = true;
                                        }
                                    }
                                    if (defPlayer != null)
                                    {
                                        int defKilled = atkLosses.Values.Sum();
                                        if (!atkWins && defKilled > 0)
                                        {
                                            defPlayer.SetDefenseScore(defPlayer.DefenseScore + defKilled);
                                            defPlayer.EarnWarPoints(defKilled);
                                            _db.Entry(defPlayer).Property(p => p.DefenseScore).IsModified = true;
                                            _db.Entry(defPlayer).Property(p => p.AvailableWarPoints).IsModified = true;
                                        }
                                    }
                                } // end normal attack / convoy-no-tp branches
                            } // end else (no active siege on target)
                        }
                    }

                    op.MarkArrived();
                    // Scout: immediately start returning so it doesn't clutter movements panel.
                    if (op.OperationType == "scout_poi")
                    {
                        int returnSeconds = (int)(op.ArrivesAtUtc - op.StartedAtUtc).TotalSeconds;
                        op.MarkReturning(returnSeconds);
                    }
                    // Settlement attack: troops automatically return — UNLESS a siege was started.
                    if (op.OperationType == "attack_settlement" && !siegeStarted)
                    {
                        int returnSecs = (int)(op.ArrivesAtUtc - op.StartedAtUtc).TotalSeconds;
                        op.MarkReturning(returnSecs);
                    }
                    // Trade convoy: deliver resources immediately on arrival, then complete.
                    if (op.OperationType == "send_resources" && op.TargetSettlementId.HasValue)
                    {
                        var tradeTarget = await _db.Settlements.Include(s => s.Buildings)
                            .FirstOrDefaultAsync(s => s.Id == op.TargetSettlementId.Value);
                        var tradeSender = await _db.Settlements
                            .FirstOrDefaultAsync(s => s.Id == op.AttackerSettlementId);
                        if (tradeTarget != null && !string.IsNullOrEmpty(op.SentUnitsJson))
                        {
                            var res = JsonSerializer.Deserialize<Dictionary<string, int>>(op.SentUnitsJson) ?? new();
                            tradeTarget.AddResourcesCapped(
                                res.GetValueOrDefault("water", 0),
                                res.GetValueOrDefault("food", 0),
                                res.GetValueOrDefault("scrap", 0),
                                res.GetValueOrDefault("fuel", 0),
                                res.GetValueOrDefault("energy", 0),
                                res.GetValueOrDefault("rareTech", 0));

                            var tradeTargetPlayer = await _db.Players
                                .FirstOrDefaultAsync(p => p.Id == tradeTarget.PlayerId);
                            if (tradeTargetPlayer != null && tradeSender != null)
                            {
                                string resList = string.Join(", ", res
                                    .Where(r => r.Value > 0)
                                    .Select(r => $"{r.Value} {char.ToUpper(r.Key[0])}{r.Key[1..]}"));
                                _db.Messages.Add(new Message(
                                    senderPlayerId: tradeTargetPlayer.Id,
                                    receiverPlayerId: tradeTargetPlayer.Id,
                                    subject: $"\ud83d\udce6 Trade Convoy Arrived \u2014 from {tradeSender.Name}",
                                    body: $"A trade convoy from {tradeSender.Name} has delivered: {resList}.",
                                    messageType: "notification"));
                            }
                        }
                        op.MarkCompleted("{}");
                    }
                    // found_settlement: convoy landed — building timer now runs (see arrived block below).
                    // reinforce_settlement stays "arrived" until recalled.
                }
                else if (op.Phase == "arrived" && op.OperationType == "found_settlement"
                    && op.ResultJson == null)
                {
                    // Convoy landed. Create the settlement once the 12-hour build timer elapses.
                    var buildEndsAt = op.ArrivesAtUtc.AddHours(12);
                    if (now >= buildEndsAt)
                    {
                        string newSettlementName = op.TargetPoiLabel ?? "New Outpost";
                        if (settlementToPlayer.TryGetValue(op.AttackerSettlementId, out var foundingPlayerId))
                        {
                            var foundingPlayer = await _db.Players
                                .Include(p => p.Settlements)
                                .FirstOrDefaultAsync(p => p.Id == foundingPlayerId);
                            if (foundingPlayer != null && foundingPlayer.Settlements.Count < foundingPlayer.MaxSettlements)
                            {
                                var newSett = new Settlement(newSettlementName, foundingPlayer.Id);
                                _db.Settlements.Add(newSett);
                                foundingPlayer.Settlements.Add(newSett);

                                foreach (var kvp in BuildingDefinitions.StarterBuildingLevels)
                                {
                                    var b = Building.CreateAtLevel(newSett.Id, kvp.Key, kvp.Value);
                                    newSett.Buildings.Add(b);
                                    _db.Buildings.Add(b);
                                }

                                if (!string.IsNullOrEmpty(op.SentUnitsJson))
                                {
                                    var convoyUnits = JsonSerializer.Deserialize<Dictionary<string, int>>(op.SentUnitsJson);
                                    if (convoyUnits != null)
                                        foreach (var (unit, qty) in convoyUnits)
                                            if (qty > 0)
                                            {
                                                newSett.UnitInventory.TryGetValue(unit, out int ex);
                                                newSett.UnitInventory[unit] = ex + qty;
                                            }
                                }

                                op.MarkCompleted(JsonSerializer.Serialize(new
                                {
                                    isFounding = true,
                                    settlementId = newSett.Id,
                                    settlementName = newSett.Name
                                }));

                                _db.Messages.Add(new Message(
                                    senderPlayerId: foundingPlayerId,
                                    receiverPlayerId: foundingPlayerId,
                                    subject: $"\U0001f3d7 Outpost Established \u2014 {newSettlementName}",
                                    body: $"Construction is complete. The outpost '{newSettlementName}' is now operational and ready for development.",
                                    messageType: "notification"));
                            }
                        }
                    }
                }
                else if (op.Phase == "arrived" && op.OperationType == "attack_settlement"
                    && op.ResultJson == null && op.TargetSettlementId.HasValue)
                {
                    // Orphaned arrived ops: battle never resolved (deployed before combat code).
                    // Resolve combat now, then immediately start returning.
                    var atkSett2 = await _db.Settlements.Include(s => s.Buildings)
                        .FirstOrDefaultAsync(s => s.Id == op.AttackerSettlementId);
                    var defSett2 = await _db.Settlements.Include(s => s.Buildings)
                        .FirstOrDefaultAsync(s => s.Id == op.TargetSettlementId.Value);
                    if (atkSett2 != null && defSett2 != null)
                    {
                        var unitDefs2 = TheFallenWastes_Domain.UnitFactory.UnitFactory.CreateStarterUnits()
                            .ToDictionary(u => u.Name, u => u, StringComparer.OrdinalIgnoreCase);
                        var sent2 = string.IsNullOrEmpty(op.SentUnitsJson)
                            ? new Dictionary<string, int>()
                            : JsonSerializer.Deserialize<Dictionary<string, int>>(op.SentUnitsJson) ?? new Dictionary<string, int>();
                        var def2 = new Dictionary<string, int>(defSett2.UnitInventory, StringComparer.OrdinalIgnoreCase);
                        int atk2 = sent2.Sum(kvp => (unitDefs2.TryGetValue(kvp.Key, out var ua) ? ua.AttackPower : 10) * kvp.Value);
                        int def2p = def2.Sum(kvp => (unitDefs2.TryGetValue(kvp.Key, out var ud) ? ud.DefenseVsBallistic : 5) * kvp.Value);
                        bool wins2 = atk2 > def2p;
                        var aLoss2 = new Dictionary<string, int>();
                        var dLoss2 = new Dictionary<string, int>();
                        if (wins2)
                        {
                            dLoss2 = def2.ToDictionary(k => k.Key, k => k.Value);
                            double lr = def2p > 0 ? Math.Min(0.85, (double)def2p / atk2) : 0.0;
                            foreach (var kvp in sent2) { int l = (int)Math.Ceiling(kvp.Value * lr); if (l > 0) aLoss2[kvp.Key] = Math.Min(l, kvp.Value); }
                        }
                        else
                        {
                            aLoss2 = sent2.ToDictionary(k => k.Key, k => k.Value);
                            double dlr = atk2 > 0 ? Math.Min(0.50, (double)atk2 / def2p) : 0.0;
                            foreach (var kvp in def2) { int l = (int)Math.Floor(kvp.Value * dlr); if (l > 0) dLoss2[kvp.Key] = l; }
                        }
                        foreach (var kvp in dLoss2)
                        {
                            if (defSett2.UnitInventory.TryGetValue(kvp.Key, out int cur2))
                            { int rem = cur2 - kvp.Value; if (rem <= 0) defSett2.UnitInventory.Remove(kvp.Key); else defSett2.UnitInventory[kvp.Key] = rem; }
                        }
                        _db.Entry(defSett2).Property(s => s.UnitInventory).IsModified = true;
                        var surv2 = sent2.ToDictionary(k => k.Key, k => k.Value - (aLoss2.TryGetValue(k.Key, out var al2) ? al2 : 0)).Where(k => k.Value > 0).ToDictionary(k => k.Key, k => k.Value);
                        var dSurv2 = def2.ToDictionary(k => k.Key, k => k.Value - (dLoss2.TryGetValue(k.Key, out var dl2) ? dl2 : 0)).Where(k => k.Value > 0).ToDictionary(k => k.Key, k => k.Value);
                        int carry2 = surv2.Sum(kvp => (unitDefs2.TryGetValue(kvp.Key, out var uc2) ? uc2.CarryCapacity : 10) * kvp.Value);
                        var loot2 = new Dictionary<string, int> { ["water"]=0, ["food"]=0, ["scrap"]=0, ["fuel"]=0, ["energy"]=0 };
                        if (wins2 && carry2 > 0)
                        {
                            int cl2 = carry2;
                            foreach (var (rn, ra) in new[] { ("water", defSett2.Resources.Water), ("food", defSett2.Resources.Food), ("scrap", defSett2.Resources.Scrap), ("fuel", defSett2.Resources.Fuel), ("energy", defSett2.Resources.Energy) })
                            { if (cl2 <= 0) break; int take2 = Math.Min((int)(ra * 0.33), cl2); if (take2 > 0) { loot2[rn] = take2; cl2 -= take2; } }
                            defSett2.Resources.Spend(water: loot2["water"], food: loot2["food"], scrap: loot2["scrap"], fuel: loot2["fuel"], energy: loot2["energy"]);
                        }
                        var bj2 = JsonSerializer.Serialize(new
                        {
                            isSettlementBattleReport = true,
                            attackerWins = wins2,
                            attackerSentUnits = sent2, attackerLosses = aLoss2, attackerSurvived = surv2,
                            defenderUnits = def2, defenderLosses = dLoss2, defenderSurvived = dSurv2,
                            lootedResources = loot2,
                            attackerSettlementName = atkSett2.Name, defenderSettlementName = defSett2.Name,
                            attackPointsGained  = wins2  ? dLoss2.Values.Sum() : 0,
                            defensePointsGained = !wins2 ? aLoss2.Values.Sum() : 0,
                        });
                        op.SetResult(bj2);
                        var ap2 = await _db.Players.FirstOrDefaultAsync(p => p.Id == atkSett2.PlayerId);
                        var dp2 = await _db.Players.FirstOrDefaultAsync(p => p.Id == defSett2.PlayerId);
                        if (ap2 != null)
                            _db.Messages.Add(new Message(ap2.Id, ap2.Id,
                                wins2 ? $"\u2694 Victory \u2014 {defSett2.Name}" : $"\u2717 Defeat \u2014 {defSett2.Name}",
                                bj2, "report"));
                        if (dp2 != null && (ap2 == null || dp2.Id != ap2.Id))
                            _db.Messages.Add(new Message(dp2.Id, dp2.Id,
                                wins2 ? $"\u26a0 Your settlement was raided by {atkSett2.Name}" : $"\u2713 Attack repelled from {atkSett2.Name}",
                                JsonSerializer.Serialize(new { isSettlementBattleReport = true, isDefenseReport = true, attackerWins = wins2, attackerSentUnits = sent2, attackerLosses = aLoss2, attackerSurvived = surv2, defenderUnits = def2, defenderLosses = dLoss2, defenderSurvived = dSurv2, lootedResources = loot2, attackerSettlementName = atkSett2.Name, defenderSettlementName = defSett2.Name, attackPointsGained = wins2 ? dLoss2.Values.Sum() : 0, defensePointsGained = !wins2 ? aLoss2.Values.Sum() : 0 }),
                                "report"));

                        // ── Score: orphaned battle ──
                        if (ap2 != null) { int k2 = dLoss2.Values.Sum(); if (wins2 && k2 > 0) { ap2.SetAttackScore(ap2.AttackScore + k2); ap2.EarnWarPoints(k2); _db.Entry(ap2).Property(p => p.AttackScore).IsModified = true; _db.Entry(ap2).Property(p => p.AvailableWarPoints).IsModified = true; } }
                        if (dp2 != null) { int k2d = aLoss2.Values.Sum(); if (!wins2 && k2d > 0) { dp2.SetDefenseScore(dp2.DefenseScore + k2d); dp2.EarnWarPoints(k2d); _db.Entry(dp2).Property(p => p.DefenseScore).IsModified = true; _db.Entry(dp2).Property(p => p.AvailableWarPoints).IsModified = true; } }
                    }
                    int retSecs2 = (int)(op.ArrivesAtUtc - op.StartedAtUtc).TotalSeconds;
                    op.MarkReturning(retSecs2);
                }
                else if (op.Phase == "arrived" && op.OperationType == "raid_poi")
                {
                    // ── Auto-return: raid mode timer expired OR all loot collected ──────
                    int raidDurSecs = op.RaidMode switch
                    {
                        "quick"      => 300,
                        "sweep"      => 900,
                        "extraction" => 3600,
                        "deep"       => 14400,
                        _            => 300
                    };
                    var poiStateAuto = await _db.PoiStates
                        .FirstOrDefaultAsync(p => p.PoiId == op.TargetPoiId);
                    var lootPoolAuto = string.IsNullOrEmpty(poiStateAuto?.LootItemsJson)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(poiStateAuto.LootItemsJson) ?? new();
                    int maxLootAuto  = lootPoolAuto.Count;
                    int earnedAuto   = Math.Min(op.CalculateLootItemsEarned(now), maxLootAuto);
                    bool timerDone   = (now - op.ArrivesAtUtc).TotalSeconds >= raidDurSecs;
                    bool allLootDone = maxLootAuto > 0 && earnedAuto >= maxLootAuto;

                    if (timerDone || allLootDone)
                    {
                        // Remove taken items from POI immediately so the live count drops at once
                        var takenItems = lootPoolAuto.Take(earnedAuto).ToList();
                        if (poiStateAuto != null && takenItems.Count > 0)
                        {
                            foreach (var takenItem in takenItems)
                                poiStateAuto.RemoveLootItem(takenItem);
                            _db.Entry(poiStateAuto).Property(p => p.LootItemsJson).IsModified = true;
                        }
                        // Merge taken item names into ResultJson so the completing tick can populate salvage
                        try
                        {
                            var resultDict = op.ResultJson != null
                                ? JsonSerializer.Deserialize<Dictionary<string, object>>(op.ResultJson,
                                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new()
                                : new Dictionary<string, object>();
                            resultDict["takenLootItems"] = (object)takenItems;
                            op.SetResult(JsonSerializer.Serialize(resultDict));
                        }
                        catch { op.SetResult(JsonSerializer.Serialize(new { takenLootItems = takenItems })); }

                        op.SetLootCollected(earnedAuto);
                        int retSecsRaid = (int)(op.ArrivesAtUtc - op.StartedAtUtc).TotalSeconds;
                        op.MarkReturning(retSecsRaid);
                    }
                }
                else if (op.Phase == "returning" && op.ReturnsAtUtc.HasValue && now >= op.ReturnsAtUtc.Value)
                {
                    // Restore surviving units and any looted resources to the home settlement
                    if (!string.IsNullOrEmpty(op.ResultJson))
                    {
                        var homeSett = await _db.Settlements
                            .Include(s => s.Buildings)
                            .FirstOrDefaultAsync(s => s.Id == op.AttackerSettlementId);
                        if (homeSett != null)
                        {
                            try
                            {
                                using var doc = JsonDocument.Parse(op.ResultJson);
                                var root = doc.RootElement;

                                if (root.TryGetProperty("attackerSurvived", out var survEl))
                                {
                                    foreach (var unit in survEl.EnumerateObject())
                                    {
                                        int qty = unit.Value.GetInt32();
                                        if (qty > 0)
                                        {
                                            homeSett.UnitInventory.TryGetValue(unit.Name, out int existing);
                                            homeSett.UnitInventory[unit.Name] = existing + qty;
                                        }
                                    }
                                    _db.Entry(homeSett).Property(s => s.UnitInventory).IsModified = true;
                                }

                                if (root.TryGetProperty("lootedResources", out var lootEl))
                                {
                                    int lw  = lootEl.TryGetProperty("water",  out var we)  ? we.GetInt32()  : 0;
                                    int lf  = lootEl.TryGetProperty("food",   out var fe)  ? fe.GetInt32()  : 0;
                                    int ls  = lootEl.TryGetProperty("scrap",  out var se)  ? se.GetInt32()  : 0;
                                    int lfu = lootEl.TryGetProperty("fuel",   out var fue) ? fue.GetInt32() : 0;
                                    int le  = lootEl.TryGetProperty("energy", out var ee)  ? ee.GetInt32()  : 0;
                                    homeSett.AddResourcesCapped(lw, lf, ls, lfu, le, 0);
                                }
                            }
                            catch { /* ignore parse errors — op still completes */ }
                        }
                    }

                    // ── POI raid: add earned loot to Salvage Inventory on return ──
                    if (op.OperationType == "raid_poi" && op.LootItemsCollected > 0)
                        await HandleRaidPoiLootCompletionAsync(op);

                    op.MarkCompleted(op.ResultJson ?? "{}");
                }
            }

            await _db.SaveChangesAsync();

            // ── Siege timer resolution ────────────────────────────────────────
            // Check for sieges whose 12-hour timer has expired and resolve conquest.
            var expiredSieges = await _db.Sieges
                .Where(s => s.Status == "Active" && DateTime.UtcNow >= s.EndsAtUtc)
                .ToListAsync();

            if (expiredSieges.Count > 0)
            {
                bool siegeResolved = false;
                foreach (var expSiege in expiredSieges)
                {
                    var besiegedSett = await _db.Settlements
                        .Include(s => s.Buildings)
                        .FirstOrDefaultAsync(s => s.Id == expSiege.SettlementId);
                    if (besiegedSett == null) { expSiege.ResolveFailed(); continue; }

                    var attackerPlayerConq = await _db.Players
                        .Include(p => p.Settlements)
                        .FirstOrDefaultAsync(p => p.Id == expSiege.AttackerPlayerId);
                    var defenderPlayerConq = await _db.Players
                        .FirstOrDefaultAsync(p => p.Id == expSiege.DefenderPlayerId);

                    if (attackerPlayerConq == null) { expSiege.ResolveFailed(); continue; }

                    // Transfer ownership
                    besiegedSett.TransferOwnership(attackerPlayerConq.Id);
                    _db.Entry(besiegedSett).Property(s => s.PlayerId).IsModified = true;
                    _db.Entry(besiegedSett).Property(s => s.UnitInventory).IsModified = true;

                    // Mark convoy operation as completed (convoy consumed)
                    var convoyOp = await _db.Operations.FirstOrDefaultAsync(o => o.Id == expSiege.ConvoyOperationId);
                    if (convoyOp != null && convoyOp.Phase == "arrived")
                        convoyOp.MarkCompleted(convoyOp.ResultJson ?? "{}");

                    expSiege.ResolveConquest();

                    // Send conquest reports
                    if (attackerPlayerConq != null)
                        _db.Messages.Add(new Message(attackerPlayerConq.Id, attackerPlayerConq.Id,
                            $"\u2605 CONQUEST — {besiegedSett.Name} is yours!",
                            $"After 12 hours of siege, {besiegedSett.Name} has fallen to your forces. The settlement now belongs to you.",
                            "notification"));
                    if (defenderPlayerConq != null)
                        _db.Messages.Add(new Message(defenderPlayerConq.Id, defenderPlayerConq.Id,
                            $"\u26a0 SETTLEMENT LOST — {besiegedSett.Name} has been conquered",
                            $"{besiegedSett.Name} was conquered by {attackerPlayerConq.Username} after a 12-hour siege.",
                            "notification"));

                    siegeResolved = true;
                }
                if (siegeResolved)
                    await _db.SaveChangesAsync();
            }

            // ── POI Relocation check ──────────────────────────────────────────
            // Only fully-cleared POIs trigger a 15-min warning then relocate.
            // Partially-raided POIs stay until fully cleared + troops gone.
            var allPoiStates = await _db.PoiStates.ToListAsync();
            var allActiveOps = await _db.Operations
                .Where(o => o.Phase == "outbound" || o.Phase == "arrived")
                .ToListAsync();

            bool relocationsTriggered = false;
            foreach (var poiState in allPoiStates)
            {
                // After 15-min warning expires: finalize (clear content + reset seed)
                if (poiState.IsRelocating && poiState.RelocatingAtUtc.HasValue &&
                    DateTime.UtcNow >= poiState.RelocatingAtUtc.Value)
                {
                    poiState.FinalizeRelocation();
                    relocationsTriggered = true;
                    continue;
                }

                if (poiState.IsRelocating) continue;

                // Only relocate POIs that have been fully cleared
                if (!poiState.IsCleared || !poiState.IsInitialized) continue;
                if (DateTime.UtcNow < poiState.NextRespawnUtc) continue;

                // Don't relocate while any troops are present or inbound
                bool hasPresence = allActiveOps.Any(o =>
                    o.TargetPoiId == poiState.PoiId &&
                    (o.Phase == "arrived" || o.Phase == "outbound"));

                if (!hasPresence)
                {
                    poiState.StartRelocationWarning(15);
                    relocationsTriggered = true;
                }
            }

            if (relocationsTriggered)
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

            // Load poi loot counts for capping LootItemsEarned in the response
            var raidPoiIds = operations
                .Where(o => o.OperationType == "raid_poi" && o.TargetPoiId != null)
                .Select(o => o.TargetPoiId!)
                .Distinct()
                .ToList();
            var poiLootCounts = raidPoiIds.Count > 0
                ? await _db.PoiStates
                    .Where(p => raidPoiIds.Contains(p.PoiId))
                    .ToDictionaryAsync(
                        p => p.PoiId,
                        p => string.IsNullOrEmpty(p.LootItemsJson)
                            ? 0
                            : (JsonSerializer.Deserialize<List<string>>(p.LootItemsJson) ?? new()).Count)
                : new Dictionary<string, int>();

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

                    int maxLootItems = o.OperationType == "raid_poi"
                        ? (o.Phase == "returning"
                            // Items were already removed from POI when the op started returning.
                            // Restore the original total so the frontend formula (max - earned = remaining) works.
                            ? o.LootItemsCollected + poiLootCounts.GetValueOrDefault(o.TargetPoiId ?? "", 0)
                            : poiLootCounts.GetValueOrDefault(o.TargetPoiId ?? "", 0))
                        : 0;

                    int raidDurationSeconds = o.RaidMode switch
                    {
                        "quick"      => 300,
                        "sweep"      => 900,
                        "extraction" => 3600,
                        "deep"       => 14400,
                        _            => 300
                    };

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
                            ? Math.Min(o.CalculateLootItemsEarned(now), maxLootItems)
                            : 0,
                        o.LootIntervalSeconds,
                        MaxLootItems = maxLootItems,
                        RaidDurationSeconds = raidDurationSeconds
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
                    && (o.Phase == "arrived" || o.Phase == "outbound"));
            if (operation == null)
                return NotFound("No active operation found to recall.");

            bool wasOutbound = operation.Phase == "outbound";
            int lootEarned = 0;
            int returnSeconds = (int)(operation.ArrivesAtUtc - operation.StartedAtUtc).TotalSeconds;

            if (!wasOutbound)
            {
                // Already at destination — collect partial loot and send back
                if (operation.OperationType == "raid_poi")
                {
                    // Cap by actual loot remaining at the POI
                    var poiStateRecall = await _db.PoiStates
                        .FirstOrDefaultAsync(p => p.PoiId == operation.TargetPoiId);
                    var lootPoolRecall = string.IsNullOrEmpty(poiStateRecall?.LootItemsJson)
                        ? new List<string>()
                        : JsonSerializer.Deserialize<List<string>>(poiStateRecall.LootItemsJson) ?? new();
                    lootEarned = Math.Min(operation.CalculateLootItemsEarned(DateTime.UtcNow), lootPoolRecall.Count);

                    // Take those items from the POI immediately (same as auto-return path)
                    var takenOnRecall = lootPoolRecall.Take(lootEarned).ToList();
                    if (poiStateRecall != null && takenOnRecall.Count > 0)
                    {
                        foreach (var item in takenOnRecall)
                            poiStateRecall.RemoveLootItem(item);
                        _db.Entry(poiStateRecall).Property(p => p.LootItemsJson).IsModified = true;
                    }

                    // Merge takenLootItems into ResultJson so HandleRaidPoiLootCompletionAsync can process them
                    try
                    {
                        var resultDict = operation.ResultJson != null
                            ? JsonSerializer.Deserialize<Dictionary<string, object>>(operation.ResultJson,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new()
                            : new Dictionary<string, object>();
                        resultDict["takenLootItems"] = (object)takenOnRecall;
                        operation.SetResult(JsonSerializer.Serialize(resultDict));
                    }
                    catch { operation.SetResult(JsonSerializer.Serialize(new { takenLootItems = takenOnRecall })); }
                }
                else
                {
                    lootEarned = operation.CalculateLootItemsEarned(DateTime.UtcNow);
                }
                operation.SetLootCollected(lootEarned);

                // For stationed reinforcement: all units return (no combat losses on recall)
                if (operation.OperationType == "reinforce_settlement")
                {
                    var allUnitsReinforce = string.IsNullOrEmpty(operation.SentUnitsJson)
                        ? new Dictionary<string, int>()
                        : JsonSerializer.Deserialize<Dictionary<string, int>>(operation.SentUnitsJson) ?? new();
                    operation.SetResult(JsonSerializer.Serialize(new
                    {
                        attackerSurvived = allUnitsReinforce,
                        lootedResources  = new { water = 0, food = 0, scrap = 0, fuel = 0, energy = 0 }
                    }));
                }
            }
            else
            {
                // En route — no loot. Mark all sent units as survivors so they are
                // restored to inventory when the returning leg completes.
                if (!string.IsNullOrEmpty(operation.SentUnitsJson))
                {
                    var allUnits = JsonSerializer.Deserialize<Dictionary<string, int>>(operation.SentUnitsJson)
                        ?? new Dictionary<string, int>();
                    var recallJson = JsonSerializer.Serialize(new
                    {
                        attackerSurvived = allUnits,
                        lootedResources  = new { water = 0, food = 0, scrap = 0, fuel = 0, energy = 0 }
                    });
                    operation.SetResult(recallJson);
                }

                // For scout ops: return the RareTech investment to the vault immediately
                if (operation.OperationType == "scout_poi"
                    && operation.ScoutRareTech.HasValue
                    && operation.ScoutRareTech.Value > 0)
                {
                    var settlement = await _db.Settlements
                        .FirstOrDefaultAsync(s => s.Id == settlementId);
                    if (settlement != null)
                    {
                        settlement.DepositToVault(operation.ScoutRareTech.Value);
                        operation.ClearScoutRareTech();
                        _db.Entry(settlement).Property(s => s.VaultRareTech).IsModified = true;
                    }
                }
            }

            operation.MarkReturning(returnSeconds);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                lootEarned,
                returnsAtUtc = operation.ReturnsAtUtc,
                message = wasOutbound
                    ? "Troops recalled en route. No loot collected."
                    : $"Troops recalled. {lootEarned} loot item(s) collected."
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

            var scoutTargetPoiState = await _db.PoiStates
                .FirstOrDefaultAsync(p => p.PoiId == request.TargetPoiId);

            // Block scout when POI is cleared (not yet respawned)
            if (scoutTargetPoiState?.IsCleared == true)
                return BadRequest("This POI has been cleared. Wait for it to respawn before scouting.");

            // Block scout when POI is in its relocation warning window
            if (scoutTargetPoiState?.IsRelocating == true)
                return BadRequest("This POI is relocating to a new location. Wait for the process to complete.");

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

            // Block attack when POI is in its relocation warning window
            var attackTargetPoiState = await _db.PoiStates
                .FirstOrDefaultAsync(p => p.PoiId == request.TargetPoiId);
            if (attackTargetPoiState?.IsRelocating == true)
                return BadRequest("This POI is relocating to a new location. Operations are disabled until it settles.");

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

        // ═══════════════════════════════════════════════════════════════════
        // POST /api/Operations/settlement/{settlementId}/reinforce-settlement
        // ═══════════════════════════════════════════════════════════════════
        [HttpPost("settlement/{settlementId}/reinforce-settlement")]
        public async Task<IActionResult> ReinforceSettlement(Guid settlementId, [FromBody] ReinforceSettlementRequest request)
        {
            var settlement = await _db.Settlements
                .Include(s => s.Buildings)
                .FirstOrDefaultAsync(s => s.Id == settlementId);

            if (settlement == null)
                return NotFound("Settlement not found.");

            if (request.TargetSettlementId == settlementId)
                return BadRequest("Cannot reinforce your own settlement.");

            if (request.Units == null || request.Units.Count == 0)
                return BadRequest("No units specified.");

            foreach (var kvp in request.Units)
            {
                settlement.UnitInventory.TryGetValue(kvp.Key, out int available);
                if (available < kvp.Value)
                    return BadRequest($"Not enough {kvp.Key} units. Available: {available}, requested: {kvp.Value}.");
            }

            // Deduct units from sending settlement
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
                operationType: "reinforce_settlement",
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
        // POST /api/Operations/settlement/{settlementId}/send-resources
        // Sends a trade convoy with resources to another player's settlement.
        // ═══════════════════════════════════════════════════════════════════
        [HttpPost("settlement/{settlementId}/send-resources")]
        public async Task<IActionResult> SendResources(Guid settlementId, [FromBody] SendResourcesRequest request)
        {
            var sourceSett = await _db.Settlements
                .Include(s => s.Buildings)
                .FirstOrDefaultAsync(s => s.Id == settlementId);
            if (sourceSett == null) return NotFound("Source settlement not found.");

            var targetSett = await _db.Settlements
                .Include(s => s.Buildings)
                .FirstOrDefaultAsync(s => s.Id == request.TargetSettlementId);
            if (targetSett == null) return NotFound("Target settlement not found.");

            if (request.TargetSettlementId == settlementId)
                return BadRequest("Cannot send resources to your own settlement.");

            int total = request.Water + request.Food + request.Scrap + request.Fuel + request.Energy + request.RareTech;
            if (total <= 0) return BadRequest("Select at least one resource to send.");

            if (!sourceSett.Resources.HasEnough(request.Water, request.Food, request.Scrap, request.Fuel, request.Energy, request.RareTech))
                return BadRequest("Not enough resources.");

            sourceSett.Resources.Spend(request.Water, request.Food, request.Scrap, request.Fuel, request.Energy, request.RareTech);

            var resourcesJson = JsonSerializer.Serialize(new Dictionary<string, int>
            {
                ["water"]   = request.Water,
                ["food"]    = request.Food,
                ["scrap"]   = request.Scrap,
                ["fuel"]    = request.Fuel,
                ["energy"]  = request.Energy,
                ["rareTech"]= request.RareTech
            });

            int travelSeconds = Math.Clamp(request.TravelSeconds, 30, 7 * 24 * 3600);
            var operation = new Operation(
                attackerSettlementId: settlementId,
                targetSettlementId: request.TargetSettlementId,
                targetPoiId: null,
                targetPoiLabel: targetSett.Name,
                operationType: "send_resources",
                sentUnitsJson: resourcesJson,
                scoutRareTech: null,
                raidMode: null,
                travelSeconds: travelSeconds);

            _db.Operations.Add(operation);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                operationId = operation.Id,
                arrivesAtUtc = operation.ArrivesAtUtc,
                message = "Trade convoy dispatched."
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

        public class ReinforceSettlementRequest
        {
            public Guid TargetSettlementId { get; set; }
            public Dictionary<string, int> Units { get; set; } = new();
            public int TravelSeconds { get; set; } = 120;
        }

        public class SendResourcesRequest
        {
            public Guid TargetSettlementId { get; set; }
            public int Water { get; set; }
            public int Food { get; set; }
            public int Scrap { get; set; }
            public int Fuel { get; set; }
            public int Energy { get; set; }
            public int RareTech { get; set; }
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

        // ─── Raid POI loot completion ───────────────────────────────────────────
        // Extracted into its own method to keep GetOperationsForSettlement's async
        // state machine small enough for the JIT to handle without crashing.
        private async Task HandleRaidPoiLootCompletionAsync(Operation op)
        {
            List<string> earnedItems = new();
            if (op.ResultJson != null)
            {
                try
                {
                    using var rd = JsonDocument.Parse(op.ResultJson);
                    if (rd.RootElement.TryGetProperty("takenLootItems", out var tiProp))
                        earnedItems = JsonSerializer.Deserialize<List<string>>(tiProp.GetRawText()) ?? new();
                }
                catch { }
            }

            if (earnedItems.Count == 0) return;

            // Ensure salvage inventory exists — query without tracking to avoid EF state confusion
            var invId = await _db.SettlementSalvageInventories
                .AsNoTracking()
                .Where(i => i.SettlementId == op.AttackerSettlementId)
                .Select(i => (Guid?)i.Id)
                .FirstOrDefaultAsync();

            if (invId == null)
            {
                var newInvId = Guid.NewGuid();
                var invNow = DateTime.UtcNow;
                await _db.Database.ExecuteSqlAsync(
                    $"INSERT INTO SettlementSalvageInventories (Id, SettlementId, StoredRareTech, StoredResearchData, UpdatedAtUtc) VALUES ({newInvId}, {op.AttackerSettlementId}, 0, 0, {invNow})");
                invId = newInvId;
            }

            var salvageNow = DateTime.UtcNow;
            foreach (var itemName in earnedItems)
            {
                var meta = GetSalvageItemMetadata(itemName);
                var newItemId = Guid.NewGuid();
                var settlementId = op.AttackerSettlementId;
                var resolvedInvId = invId.Value;
                var sourceType = "poi";
                string? noSpecialOutput = null;
                await _db.Database.ExecuteSqlAsync(
                    $@"MERGE SalvageItems WITH (HOLDLOCK) AS target
                    USING (SELECT {settlementId} AS SettlementId, {meta.Key} AS [Key]) AS source
                    ON target.SettlementId = source.SettlementId AND target.[Key] = source.[Key]
                    WHEN MATCHED THEN
                        UPDATE SET [Quantity] = target.[Quantity] + 1, [AcquiredAtUtc] = {salvageNow}
                    WHEN NOT MATCHED THEN
                        INSERT ([Id],[SettlementId],[SettlementSalvageInventoryId],[Key],[Name],[Description],[SourceType],[Rarity],[Quantity],[RequiredTechSalvagerLevel],[BaseSalvageTimeSeconds],[RareTechYield],[ResearchDataYield],[SpecialOutputKey],[AcquiredAtUtc])
                        VALUES ({newItemId},{settlementId},{resolvedInvId},{meta.Key},{meta.Name},{meta.Description},{sourceType},{meta.Rarity},1,{meta.RequiredLevel},{meta.SalvageTimeSeconds},{meta.RareTechYield},0,{noSpecialOutput},{salvageNow});");
            }

            await _db.Database.ExecuteSqlAsync(
                $"UPDATE SettlementSalvageInventories SET UpdatedAtUtc = {salvageNow} WHERE Id = {invId.Value}");

            // Clear takenLootItems from ResultJson so the next poll won't re-salvage
            try
            {
                var resultDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    op.ResultJson!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                resultDict.Remove("takenLootItems");
                op.SetResult(JsonSerializer.Serialize(resultDict));
            }
            catch { }

            // Send loot return report
            var lootSett = await _db.Settlements
                .FirstOrDefaultAsync(s => s.Id == op.AttackerSettlementId);
            if (lootSett != null)
            {
                var lootPlayer = await _db.Players
                    .FirstOrDefaultAsync(p => p.Id == lootSett.PlayerId);
                if (lootPlayer != null)
                {
                    string poiNameLoot = !string.IsNullOrEmpty(op.TargetPoiLabel)
                        ? op.TargetPoiLabel : op.TargetPoiId ?? "Unknown POI";
                    _db.Messages.Add(new Message(
                        senderPlayerId: lootPlayer.Id,
                        receiverPlayerId: lootPlayer.Id,
                        subject: $"\ud83e\uddfa Loot Returned \u2014 {poiNameLoot}",
                        body: JsonSerializer.Serialize(new
                        {
                            isLootReport = true,
                            poiName = poiNameLoot,
                            itemsCollected = earnedItems.Count,
                            items = earnedItems
                        }),
                        messageType: "report"));
                }
            }
        }

        // ─── Salvage item metadata lookup ──────────────────────────────────────
        private record SalvageItemMeta(string Key, string Name, string Description, string Rarity, int RequiredLevel, int SalvageTimeSeconds, int RareTechYield);

        private static SalvageItemMeta GetSalvageItemMetadata(string itemName)
        {
            return itemName switch
            {
                "Cracked Circuit Board"    => new("cracked_circuit_board",    "Cracked Circuit Board",    "Damaged pre-fall electronics board.",                      "common",    1, 120, 2),
                "Burned Power Cell"        => new("burned_power_cell",        "Burned Power Cell",        "A discharged power cell recovered from ruins.",            "common",    1, 120, 2),
                "Scrap Bundle"             => new("scrap_bundle",             "Scrap Bundle",             "Assorted wasteland scrap with salvage potential.",         "common",    1, 60,  1),
                "Fractured Optics Module"  => new("fractured_optics_module",  "Fractured Optics Module",  "A damaged optical sensor from a pre-fall drone.",          "common",    1, 180, 3),
                "Damaged Servo Bundle"     => new("damaged_servo_bundle",     "Damaged Servo Bundle",     "A set of degraded servo motors from old machinery.",       "uncommon",  1, 240, 5),
                "Broken Drone Core"        => new("broken_drone_core",        "Broken Drone Core",        "Core unit of a destroyed recon drone.",                    "uncommon",  1, 300, 6),
                "Pre-War Guidance Chip"    => new("pre_war_guidance_chip",    "Pre-War Guidance Chip",    "Intact guidance chip from pre-fall military hardware.",    "uncommon",  1, 300, 7),
                "Encrypted Datacore"       => new("encrypted_datacore",       "Encrypted Datacore",       "Recovered archive core with fragmented technical records.", "rare",     2, 480, 8),
                "Ancient Data Core"        => new("ancient_data_core",        "Ancient Data Core",        "High-density archive from a pre-fall facility.",           "rare",      2, 480, 10),
                "Prototype Schematic"      => new("prototype_schematic",      "Prototype Schematic",      "A damaged blueprint for advanced military engineering.",    "epic",      2, 720, 16),
                "Reactor Fragment"         => new("reactor_fragment",         "Reactor Fragment",         "Unstable power shard from a collapsed industrial zone.",   "rare",      2, 600, 5),
                "Vault Artifact"           => new("vault_artifact",           "Vault Artifact",           "Intact pre-fall relic from a sealed underground vault.",   "legendary", 3, 1200, 24),
                _                          => new(itemName.ToLowerInvariant().Replace(" ", "_"), itemName, "Unknown salvage item.", "common", 1, 120, 2),
            };
        }
    }
}
