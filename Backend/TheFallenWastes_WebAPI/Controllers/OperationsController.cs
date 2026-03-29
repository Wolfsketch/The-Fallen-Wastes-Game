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

                                // ── Score: POI raid attack score + TP ──
                                if (attackerWins)
                                {
                                    int killed = npcLosses.Values.Sum();
                                    if (killed > 0)
                                    {
                                        raidAttackerPlayer.SetAttackScore(raidAttackerPlayer.AttackScore + killed);
                                        raidAttackerPlayer.AddTriumphPoints(killed);
                                        _db.Entry(raidAttackerPlayer).Property(p => p.AttackScore).IsModified = true;
                                        _db.Entry(raidAttackerPlayer).Property(p => p.TriumphPoints).IsModified = true;
                                    }
                                }
                            }
                        }
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

                        if (atkSett != null && defSett != null)
                        {
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
                            });
                            op.SetResult(battleJson);

                            // Reports
                            var atkPlayer = await _db.Players.FirstOrDefaultAsync(p => p.Id == atkSett.PlayerId);
                            var defPlayer = await _db.Players.FirstOrDefaultAsync(p => p.Id == defSett.PlayerId);

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
                                    atkPlayer.AddTriumphPoints(atkKilled);
                                    _db.Entry(atkPlayer).Property(p => p.AttackScore).IsModified = true;
                                    _db.Entry(atkPlayer).Property(p => p.TriumphPoints).IsModified = true;
                                }
                            }
                            if (defPlayer != null)
                            {
                                int defKilled = atkLosses.Values.Sum();
                                if (!atkWins && defKilled > 0)
                                {
                                    defPlayer.SetDefenseScore(defPlayer.DefenseScore + defKilled);
                                    defPlayer.AddTriumphPoints(defKilled);
                                    _db.Entry(defPlayer).Property(p => p.DefenseScore).IsModified = true;
                                    _db.Entry(defPlayer).Property(p => p.TriumphPoints).IsModified = true;
                                }
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
                    // Settlement attack: troops automatically return (no garrison without convoy)
                    if (op.OperationType == "attack_settlement")
                    {
                        int returnSecs = (int)(op.ArrivesAtUtc - op.StartedAtUtc).TotalSeconds;
                        op.MarkReturning(returnSecs);
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
                                JsonSerializer.Serialize(new { isSettlementBattleReport = true, isDefenseReport = true, attackerWins = wins2, attackerSentUnits = sent2, attackerLosses = aLoss2, attackerSurvived = surv2, defenderUnits = def2, defenderLosses = dLoss2, defenderSurvived = dSurv2, lootedResources = loot2, attackerSettlementName = atkSett2.Name, defenderSettlementName = defSett2.Name }),
                                "report"));

                        // ── Score: orphaned battle ──
                        if (ap2 != null) { int k2 = dLoss2.Values.Sum(); if (wins2 && k2 > 0) { ap2.SetAttackScore(ap2.AttackScore + k2); ap2.AddTriumphPoints(k2); _db.Entry(ap2).Property(p => p.AttackScore).IsModified = true; _db.Entry(ap2).Property(p => p.TriumphPoints).IsModified = true; } }
                        if (dp2 != null) { int k2d = aLoss2.Values.Sum(); if (!wins2 && k2d > 0) { dp2.SetDefenseScore(dp2.DefenseScore + k2d); dp2.AddTriumphPoints(k2d); _db.Entry(dp2).Property(p => p.DefenseScore).IsModified = true; _db.Entry(dp2).Property(p => p.TriumphPoints).IsModified = true; } }
                    }
                    int retSecs2 = (int)(op.ArrivesAtUtc - op.StartedAtUtc).TotalSeconds;
                    op.MarkReturning(retSecs2);
                }
                else if (op.Phase == "returning" && op.ReturnsAtUtc.HasValue && now >= op.ReturnsAtUtc.Value)
                {
                    // Settlement attack return: restore surviving units + loot to attacker
                    if (op.OperationType == "attack_settlement"
                        && !string.IsNullOrEmpty(op.ResultJson))
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
                    op.MarkCompleted(op.ResultJson ?? "{}");
                }
            }

            await _db.SaveChangesAsync();

            // ── POI Relocation check ──────────────────────────────────────────
            // Every 12 h, if a POI has no players present or inbound, relocate it.
            var allPoiStates = await _db.PoiStates.ToListAsync();
            var allActiveOps = await _db.Operations
                .Where(o => o.Phase == "outbound" || o.Phase == "arrived")
                .ToListAsync();

            bool relocationsTriggered = false;
            foreach (var poiState in allPoiStates)
            {
                // Complete the 3-second grace period for any already-relocating POI
                if (poiState.IsRelocating && poiState.RelocatingAtUtc.HasValue &&
                    (DateTime.UtcNow - poiState.RelocatingAtUtc.Value).TotalSeconds >= 3)
                {
                    poiState.CompleteRelocation();
                    relocationsTriggered = true;
                    continue;
                }

                if (poiState.IsRelocating) continue;

                // Only consider initialized POIs whose 12-hour window has opened
                if (!poiState.IsInitialized || DateTime.UtcNow < poiState.NextRespawnUtc)
                    continue;

                bool hasArrived = allActiveOps.Any(o =>
                    o.TargetPoiId == poiState.PoiId && o.Phase == "arrived");
                bool hasInbound = allActiveOps.Any(o =>
                    o.TargetPoiId == poiState.PoiId && o.Phase == "outbound");

                if (!hasArrived && !hasInbound)
                {
                    poiState.TriggerRelocation();
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
