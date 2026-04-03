using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheFallenWastes_Application;
using TheFallenWastes_Application.Services;
using TheFallenWastes_Domain.Entities;
using TheFallenWastes_Domain.Enums;
using TheFallenWastes_Domain.GameData;
using TheFallenWastes_Domain.UnitFactory;
using TheFallenWastes_Infrastructure;

namespace TheFallenWastes_WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettlementsController : ControllerBase
    {
        private readonly GameDbContext _db;
        private readonly PlayerDataMigrationService _migrationService;

        public SettlementsController(GameDbContext db, PlayerDataMigrationService migrationService)
        {
            _db = db;
            _migrationService = migrationService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSettlement(Guid id)
        {
            var settlement = await _db.Settlements
                .Include(s => s.Buildings)
                .Include(s => s.TrainingQueue)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (settlement == null)
                return NotFound("Settlement not found.");

            EnsureStarterBuildings(settlement);

            var player = await _db.Players.FirstOrDefaultAsync(p => p.Id == settlement.PlayerId);

            bool wasMigrated = false;
            if (player != null)
            {
                wasMigrated = _migrationService.MigratePlayer(player, settlement, _db);
                if (wasMigrated)
                    await _db.SaveChangesAsync();
            }

            ResourceTickService.ApplyResourceTick(settlement, settlement.Buildings);

            foreach (var b in settlement.Buildings)
                b.TryCompleteConstruction();

            CompleteReadyUnitTrainingInternal(settlement);

            await StartNextQueuedConstructionsInternal(settlement);

            settlement.RecalculateUsedPopulation();

            settlement.Resources.CapToStorage(
                settlement.WaterCapacity,
                settlement.FoodCapacity,
                settlement.ScrapCapacity,
                settlement.FuelCapacity,
                settlement.EnergyCapacity,
                settlement.RareTechCapacity
            );

            await _db.SaveChangesAsync();

            var production = ResourceTickService.GetTotalProduction(settlement.Buildings);
            var currentLevels = GetCurrentLevels(settlement);

            int buildQueueLimit = player?.GetBuildQueueLimit() ?? 2;
            int activeBuildCount = settlement.Buildings.Count(b => b.IsConstructing);
            bool buildQueueFull = activeBuildCount >= buildQueueLimit;

            var waitingCount = await _db.BuildingUpgradeQueueItems
                .Where(q => q.SettlementId == settlement.Id && !q.IsStarted)
                .CountAsync();

            int trainingQueueLimit = player?.GetBuildQueueLimit() ?? 2;
            var activeTrainingQueue = settlement.TrainingQueue
                .Where(q => !q.IsCompleted)
                .OrderBy(q => q.EndsAtUtc)
                .ToList();

            int totalBuildingPower = settlement.Buildings.Sum(b => BuildingDefinitions.GetPowerValue(b.Type, b.Level));
            int totalBuildingPopulationUsage = settlement.Buildings.Sum(b => BuildingDefinitions.GetPopulationUsage(b.Type, b.Level));

            return Ok(new
            {
                settlement.Id,
                settlement.Name,
                settlement.PlayerId,
                settlement.UsedPopulation,
                settlement.PopulationCapacity,
                settlement.AvailablePopulation,
                settlement.Morale,
                settlement.UnitInventory,
                settlement.VaultRareTech,
                RaidVaultCapacity = settlement.RaidVaultCapacity,
                RaidVaultLevel = settlement.GetBuildingLevel(BuildingType.RaidVault),

                Resources = new
                {
                    settlement.Resources.Water,
                    settlement.Resources.Food,
                    settlement.Resources.Scrap,
                    settlement.Resources.Fuel,
                    settlement.Resources.Energy,
                    settlement.Resources.RareTech
                },

                Storage = new
                {
                    settlement.WaterCapacity,
                    settlement.FoodCapacity,
                    settlement.ScrapCapacity,
                    settlement.FuelCapacity,
                    settlement.EnergyCapacity,
                    settlement.RareTechCapacity
                },

                Production = new
                {
                    production.Water,
                    production.Food,
                    production.Scrap,
                    production.Fuel,
                    production.Energy,
                    production.RareTech
                },

                Power = new
                {
                    TotalBuildingPower = totalBuildingPower
                },

                Population = new
                {
                    settlement.PopulationCapacity,
                    settlement.UsedPopulation,
                    settlement.AvailablePopulation,
                    BuildingPopulationUsage = totalBuildingPopulationUsage
                },

                BuildQueue = new
                {
                    Active = activeBuildCount,
                    Limit = buildQueueLimit,
                    Waiting = waitingCount,
                    CommanderActive = player?.CommanderActive ?? false,
                    CommanderExpiresUtc = player?.CommanderExpiresUtc
                },

                TrainingQueue = new
                {
                    Active = activeTrainingQueue.Count,
                    Limit = trainingQueueLimit,
                    CommanderActive = player?.CommanderActive ?? false,
                    CommanderExpiresUtc = player?.CommanderExpiresUtc,
                    Items = activeTrainingQueue.Select(q => new
                    {
                        q.Id,
                        q.UnitName,
                        q.Quantity,
                        q.StartedAtUtc,
                        q.EndsAtUtc,
                        RemainingSeconds = q.GetRemainingSeconds(),
                        PopulationCostPerUnit = q.PopulationCostPerUnit
                    }).ToList()
                },

                BuildingCount = settlement.Buildings.Count(b => b.Level > 0),

                Buildings = settlement.Buildings
                    .OrderBy(b => BuildingDefinitions.GetCategory(b.Type))
                    .ThenBy(b => b.Type)
                    .Select(b => MapBuilding(b, currentLevels, buildQueueFull))
            });
        }

        [HttpPost("{id}/vault/deposit")]
        public async Task<IActionResult> DepositToVault(Guid id, [FromBody] DepositRequest request)
        {
            var settlement = await _db.Settlements
                .Include(s => s.Buildings)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (settlement == null) return NotFound("Settlement not found.");

            int raidVaultLevel = settlement.GetBuildingLevel(BuildingType.RaidVault);
            if (raidVaultLevel < 1)
                return BadRequest("Build a Relic Vault first.");

            var (deposited, error) = settlement.DepositToVault(request.Amount);
            if (error != null) return BadRequest(error);

            await _db.SaveChangesAsync();

            return Ok(new
            {
                Deposited = deposited,
                VaultRareTech = settlement.VaultRareTech,
                VaultCapacity = settlement.RaidVaultCapacity,
                RemainingRareTech = settlement.Resources.RareTech,
                Message = $"{deposited} RT deposited into Relic Vault."
            });
        }

        // ── Rename settlement ──────────────────────────────────────────────
        [HttpPatch("{id}/rename")]
        public async Task<IActionResult> RenameSettlement(Guid id, [FromBody] RenameSettlementRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name) || req.Name.Length < 3 || req.Name.Length > 40)
                return BadRequest("Settlement name must be 3–40 characters.");

            if (!Request.Headers.TryGetValue("X-Player-Id", out var pidHeader)
                || !Guid.TryParse(pidHeader, out var playerId))
                return BadRequest("X-Player-Id header required.");

            var settlement = await _db.Settlements.FindAsync(id);
            if (settlement == null) return NotFound("Settlement not found.");
            if (settlement.PlayerId != playerId) return Forbid();

            settlement.Rename(req.Name.Trim());
            await _db.SaveChangesAsync();
            return Ok(new { settlement.Id, settlement.Name });
        }

        // ── Found new settlement ───────────────────────────────────────────
        // Instead of creating the settlement instantly, dispatches a 12-hour founding convoy.
        // Units are deducted from the source settlement immediately. When the operation
        // resolves in OperationsController, the new settlement is created with the convoy units.
        [HttpPost("found")]
        public async Task<IActionResult> FoundSettlement([FromBody] FoundSettlementRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name) || req.Name.Trim().Length < 3 || req.Name.Trim().Length > 40)
                return BadRequest("Settlement name must be 3–40 characters.");

            var player = await _db.Players
                .Include(p => p.Settlements)
                .FirstOrDefaultAsync(p => p.Id == req.PlayerId);
            if (player == null) return NotFound("Player not found.");

            if (player.Settlements.Count >= player.MaxSettlements)
            {
                int needed = player.TriumphPointsForNextLevel;
                return BadRequest(new
                {
                    error = "conquest_level_required",
                    message = $"You need {needed - player.TriumphPoints} more Triumph Points to found another outpost.",
                    currentLevel = player.ConquestLevel,
                    triumphPoints = player.TriumphPoints,
                    triumphPointsNeeded = needed
                });
            }

            // Reject if ANY founding convoy is already targeting this sector (from any player)
            string sectorId = $"sector_{Math.Round(req.SectorX)}_{Math.Round(req.SectorY)}";
            bool sectorTaken = await _db.Operations.AnyAsync(o =>
                o.TargetPoiId == sectorId &&
                o.OperationType == "found_settlement" &&
                o.Phase != "completed");
            if (sectorTaken)
                return BadRequest(new { message = "This location is already claimed by another founding convoy or is currently under construction. Choose a different sector." });

            // Reject if a founding convoy is already in flight from this player's settlement
            bool alreadyFounding = await _db.Operations.AnyAsync(o =>
                o.AttackerSettlementId == req.SourceSettlementId &&
                o.OperationType == "found_settlement" &&
                o.Phase != "completed");
            if (alreadyFounding)
                return BadRequest(new { message = "A founding convoy is already en route from this settlement." });

            var sourceSettlement = await _db.Settlements
                .FirstOrDefaultAsync(s => s.Id == req.SourceSettlementId && s.PlayerId == req.PlayerId);
            if (sourceSettlement == null) return NotFound("Source settlement not found.");

            // Validate convoy — at least 1 unit required
            var convoy = req.Convoy ?? new Dictionary<string, int>();
            int totalConvoy = convoy.Where(kvp => kvp.Value > 0).Sum(kvp => kvp.Value);
            if (totalConvoy < 1)
                return BadRequest("You must send at least 1 unit as a founding convoy.");

            foreach (var (unit, qty) in convoy)
            {
                if (qty <= 0) continue;
                sourceSettlement.UnitInventory.TryGetValue(unit, out int available);
                if (available < qty)
                    return BadRequest($"Not enough {unit} — you have {available}, convoy needs {qty}.");
            }

            // Deduct convoy units from source settlement
            foreach (var (unit, qty) in convoy)
            {
                if (qty <= 0) continue;
                sourceSettlement.UnitInventory[unit] -= qty;
                if (sourceSettlement.UnitInventory[unit] <= 0)
                    sourceSettlement.UnitInventory.Remove(unit);
            }
            _db.Entry(sourceSettlement).Property(s => s.UnitInventory).IsModified = true;

            // Create the founding operation — travel time from frontend, then 12h build after arrival
            int travelSeconds = Math.Clamp(req.TravelSeconds, 60, 7 * 24 * 3600);
            var convoyJson = System.Text.Json.JsonSerializer.Serialize(
                convoy.Where(kvp => kvp.Value > 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            var operation = new Operation(
                attackerSettlementId: req.SourceSettlementId,
                targetSettlementId: null,
                targetPoiId: sectorId,              // sector identifier for conflict detection
                targetPoiLabel: req.Name.Trim(),    // settlement name
                operationType: "found_settlement",
                sentUnitsJson: convoyJson,
                scoutRareTech: null,
                raidMode: null,
                travelSeconds: travelSeconds);
            _db.Operations.Add(operation);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                operationId = operation.Id,
                arrivesAtUtc = operation.ArrivesAtUtc,
                buildsAtUtc = operation.ArrivesAtUtc.AddHours(12),
                message = $"Founding convoy dispatched! Convoy arrives in {formatSeconds(travelSeconds)}, then 12 hours to establish '{req.Name.Trim()}'.",
            });
        }

        static string formatSeconds(int s) => s < 3600 ? $"{s / 60}m" : $"{s / 3600}h {s % 3600 / 60}m";

        public record RenameSettlementRequest(string Name);
        public record FoundSettlementRequest(Guid PlayerId, Guid SourceSettlementId, string Name, Dictionary<string, int>? Convoy, int TravelSeconds, double SectorX, double SectorY);

        [HttpGet("{id}/queue")]
        public async Task<IActionResult> GetBuildQueue(Guid id)
        {
            var settlement = await _db.Settlements
                .Include(s => s.Buildings)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (settlement == null)
                return NotFound("Settlement not found.");

            foreach (var b in settlement.Buildings)
                b.TryCompleteConstruction();

            await StartNextQueuedConstructionsInternal(settlement);

            settlement.RecalculateUsedPopulation();

            settlement.Resources.CapToStorage(
                settlement.WaterCapacity,
                settlement.FoodCapacity,
                settlement.ScrapCapacity,
                settlement.FuelCapacity,
                settlement.EnergyCapacity,
                settlement.RareTechCapacity
            );

            await _db.SaveChangesAsync();

            var player = await _db.Players.FindAsync(settlement.PlayerId);
            int queueLimit = player?.GetBuildQueueLimit() ?? 2;

            var constructing = settlement.Buildings
                .Where(b => b.IsConstructing)
                .OrderBy(b => b.ConstructionEndUtc)
                .ToList();

            // Load started queue items to provide snapshot cost / refund preview for active constructions
            var startedQueueItems = await _db.BuildingUpgradeQueueItems
                .Where(q => q.SettlementId == settlement.Id && q.IsStarted)
                .ToListAsync();

            var constructingWithCost = constructing
                .Select(b =>
                {
                    // try match by ActiveBuildingId first
                    var match = startedQueueItems.FirstOrDefault(q => q.ActiveBuildingId == b.Id);
                    if (match == null)
                    {
                        // fallback: match by building type and target level
                        match = startedQueueItems.FirstOrDefault(q => q.BuildingType == b.Type && q.TargetLevel == b.TargetLevel);
                    }

                    object costObj;
                    object refundPreview;
                    if (match != null)
                    {
                        costObj = new
                        {
                            water = match.CostWater,
                            food = match.CostFood,
                            scrap = match.CostScrap,
                            fuel = match.CostFuel,
                            energy = match.CostEnergy,
                            rareTech = match.CostRareTech
                        };
                        refundPreview = new
                        {
                            water = (int)Math.Ceiling(match.CostWater * 0.75),
                            food = (int)Math.Ceiling(match.CostFood * 0.75),
                            scrap = (int)Math.Ceiling(match.CostScrap * 0.75),
                            fuel = (int)Math.Ceiling(match.CostFuel * 0.75),
                            energy = (int)Math.Ceiling(match.CostEnergy * 0.75),
                            rareTech = (int)Math.Ceiling(match.CostRareTech * 0.75)
                        };
                    }
                    else
                    {
                        // Fallback: calculate approximate cost using building definitions
                        var cost = BuildingDefinitions.GetUpgradeCost(b.Type, b.TargetLevel);
                        costObj = new
                        {
                            water = cost.Water,
                            food = cost.Food,
                            scrap = cost.Scrap,
                            fuel = cost.Fuel,
                            energy = cost.Energy,
                            rareTech = cost.RareTech
                        };
                        refundPreview = new
                        {
                            water = (int)Math.Ceiling(cost.Water * 0.75),
                            food = (int)Math.Ceiling(cost.Food * 0.75),
                            scrap = (int)Math.Ceiling(cost.Scrap * 0.75),
                            fuel = (int)Math.Ceiling(cost.Fuel * 0.75),
                            energy = (int)Math.Ceiling(cost.Energy * 0.75),
                            rareTech = (int)Math.Ceiling(cost.RareTech * 0.75)
                        };
                    }

                    return new
                    {
                        b.Id,
                        Type = b.Type.ToString(),
                        DisplayName = BuildingDefinitions.GetDisplayName(b.Type),
                        b.Level,
                        b.TargetLevel,
                        b.ConstructionEndUtc,
                        RemainingSeconds = b.GetRemainingSeconds(),
                        BuildTimeSeconds = BuildingDefinitions.GetBuildTimeSeconds(b.Type, b.TargetLevel),
                        Cost = costObj,
                        RefundPreview = refundPreview
                    };
                })
                .ToList();

            var waitingItems = await _db.BuildingUpgradeQueueItems
                .Where(q => q.SettlementId == settlement.Id && !q.IsStarted)
                .OrderBy(q => q.CreatedAtUtc)
                .ToListAsync();

            var waiting = waitingItems
                .Select(q => new
                {
                    q.Id,
                    Type = q.BuildingType.ToString(),
                    DisplayName = BuildingDefinitions.GetDisplayName(q.BuildingType),
                    q.TargetLevel,
                    q.CreatedAtUtc,
                    Cost = new
                    {
                        water = q.CostWater,
                        food = q.CostFood,
                        scrap = q.CostScrap,
                        fuel = q.CostFuel,
                        energy = q.CostEnergy,
                        rareTech = q.CostRareTech
                    }
                })
                .ToList();

            return Ok(new
            {
                Active = constructing.Count,
                Limit = queueLimit,
                SlotsAvailable = Math.Max(0, queueLimit - constructing.Count),
                CommanderActive = player?.CommanderActive ?? false,
                CommanderExpiresUtc = player?.CommanderExpiresUtc,
                Constructing = constructingWithCost,
                Waiting = waiting
            });
        }

        [HttpGet("{id}/buildings")]
        public async Task<IActionResult> GetBuildings(Guid id)
        {
            var settlement = await _db.Settlements
                .Include(s => s.Buildings)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (settlement == null)
                return NotFound("Settlement not found.");

            EnsureStarterBuildings(settlement);

            foreach (var b in settlement.Buildings)
                b.TryCompleteConstruction();

            await StartNextQueuedConstructionsInternal(settlement);

            settlement.RecalculateUsedPopulation();

            await _db.SaveChangesAsync();

            var currentLevels = GetCurrentLevels(settlement);

            var player = await _db.Players.FindAsync(settlement.PlayerId);
            int queueLimit = player?.GetBuildQueueLimit() ?? 2;
            int activeCount = settlement.Buildings.Count(b => b.IsConstructing);

            // Gather queued info per building type so UI can decide about chained queuing
            var allQueued = await _db.BuildingUpgradeQueueItems
                .Where(q => q.SettlementId == settlement.Id)
                .ToListAsync();
            int totalWaiting = allQueued.Count(q => !q.IsStarted);
            bool queueFull = (activeCount + totalWaiting) >= queueLimit;
            var waitingCounts = allQueued.Where(q => !q.IsStarted).GroupBy(q => q.BuildingType)
                .ToDictionary(g => g.Key, g => g.Count());
            var highestQueuedTarget = allQueued.GroupBy(q => q.BuildingType)
                .ToDictionary(g => g.Key, g => g.Max(x => x.TargetLevel));

            var result = Enum.GetValues<BuildingType>()
                .Select(type =>
                {
                    var existing = settlement.Buildings.FirstOrDefault(b => b.Type == type);
                    int level = existing?.Level ?? BuildingDefinitions.GetStartingLevel(type);
                    int queuedMax = highestQueuedTarget.GetValueOrDefault(type, 0);
                    int baseLevel = Math.Max(level, existing?.TargetLevel ?? 0);
                    int nextLevel = Math.Max(baseLevel, queuedMax) + 1;
                    bool prerequisitesMet = BuildingDefinitions.ArePrerequisitesMet(type, currentLevels);
                    var prereqs = BuildingDefinitions.GetPrerequisites(type);
                    bool isFutureFeature = BuildingDefinitions.IsFutureFeature(type);
                    bool isBuildable = BuildingDefinitions.IsBuildable(type);

                    return new
                    {
                        Id = existing?.Id,
                        Type = type.ToString(),
                        DisplayName = BuildingDefinitions.GetDisplayName(type),
                        Description = BuildingDefinitions.GetDescription(type),
                        Category = BuildingDefinitions.GetCategory(type),

                        StartingLevel = BuildingDefinitions.GetStartingLevel(type),
                        Level = existing?.Level ?? 0,
                        EffectiveLevel = level,

                        IsConstructing = existing?.IsConstructing ?? false,
                        TargetLevel = existing?.TargetLevel ?? 0,
                        RemainingSeconds = existing?.GetRemainingSeconds() ?? 0,
                        ConstructionEndUtc = existing?.ConstructionEndUtc,

                        IsFutureFeature = isFutureFeature,
                        IsBuildable = isBuildable,
                        IsLocked = (!prerequisitesMet && (existing?.Level ?? 0) == 0) || isFutureFeature,

                        Prerequisites = prereqs?.Select(p => new
                        {
                            Type = p.RequiredType.ToString(),
                            DisplayName = BuildingDefinitions.GetDisplayName(p.RequiredType),
                            RequiredLevel = p.RequiredLevel,
                            CurrentLevel = currentLevels.GetValueOrDefault(p.RequiredType, 0),
                            IsMet = currentLevels.GetValueOrDefault(p.RequiredType, 0) >= p.RequiredLevel
                        }),

                        // allow queuing upgrades for same building even when active; frontend should validate resources
                        CanUpgrade = isBuildable
                            && prerequisitesMet
                            && nextLevel <= BuildingDefinitions.MaxBuildingLevel,

                        // overall queue full indicates no free active slots; queuing still allowed (waiting)
                        QueueFull = queueFull,
                        QueuedCount = waitingCounts.GetValueOrDefault(type, 0),
                        NextQueuedLevel = queuedMax > 0 ? queuedMax + 1 : (int?)null,

                        UpgradeCost = isBuildable && nextLevel <= BuildingDefinitions.MaxBuildingLevel
                            ? BuildingDefinitions.GetUpgradeCost(type, nextLevel)
                            : null,

                        BuildTimeSeconds = isBuildable && nextLevel <= BuildingDefinitions.MaxBuildingLevel
                            ? BuildingDefinitions.GetBuildTimeSeconds(type, nextLevel)
                            : 0,

                        HourlyProduction = BuildingDefinitions.GetHourlyProduction(type, level),
                        PopulationUsage = BuildingDefinitions.GetPopulationUsage(type, level),
                        PowerValue = BuildingDefinitions.GetPowerValue(type, level),
                        StorageBonus = BuildingDefinitions.GetStorageBonus(type, level),
                        DefenseValue = BuildingDefinitions.GetDefenseValue(type, level)
                    };
                })
                .OrderBy(b => b.IsLocked)
                .ThenBy(b => b.Category)
                .ThenBy(b => b.Type);

            return Ok(result);
        }

        [HttpPost("{id}/buildings/upgrade")]
        public async Task<IActionResult> UpgradeBuilding(Guid id, [FromBody] UpgradeBuildingRequest request)
        {
            if (!Enum.TryParse<BuildingType>(request.BuildingType, out var buildingType))
                return BadRequest($"Unknown building type: {request.BuildingType}");

            if (!BuildingDefinitions.IsBuildable(buildingType))
                return BadRequest($"{BuildingDefinitions.GetDisplayName(buildingType)} is not buildable yet.");

            var settlement = await _db.Settlements
                .Include(s => s.Buildings)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (settlement == null)
                return NotFound("Settlement not found.");

            EnsureStarterBuildings(settlement);

            ResourceTickService.ApplyResourceTick(settlement, settlement.Buildings);

            foreach (var b in settlement.Buildings)
                b.TryCompleteConstruction();

            settlement.RecalculateUsedPopulation();

            var currentLevels = GetCurrentLevels(settlement);
            if (!BuildingDefinitions.ArePrerequisitesMet(buildingType, currentLevels))
            {
                var prereqs = BuildingDefinitions.GetPrerequisites(buildingType);
                var missing = prereqs?
                    .Where(p => currentLevels.GetValueOrDefault(p.RequiredType, 0) < p.RequiredLevel)
                    .Select(p => $"{BuildingDefinitions.GetDisplayName(p.RequiredType)} L{p.RequiredLevel}");

                return BadRequest($"Prerequisites not met. Requires: {string.Join(", ", missing ?? Array.Empty<string>())}");
            }

            var building = settlement.Buildings.FirstOrDefault(b => b.Type == buildingType);

            if (building == null)
            {
                int startingLevel = BuildingDefinitions.GetStartingLevel(buildingType);
                building = startingLevel > 0
                    ? Building.CreateAtLevel(settlement.Id, buildingType, startingLevel)
                    : new Building(settlement.Id, buildingType);

                _db.Buildings.Add(building);
                settlement.Buildings.Add(building);
            }

            building.TryCompleteConstruction();

            // Determine the next target level taking into account existing queued items
            var existingQueued = await _db.BuildingUpgradeQueueItems
                .Where(q => q.SettlementId == settlement.Id && q.BuildingType == buildingType && !q.IsStarted)
                .ToListAsync();

            int highestQueuedTarget = existingQueued.Any() ? existingQueued.Max(q => q.TargetLevel) : 0;
            int currentBase = building != null ? Math.Max(building.Level, building.TargetLevel) : BuildingDefinitions.GetStartingLevel(buildingType);
            int targetLevel = Math.Max(currentBase, highestQueuedTarget) + 1;
            if (targetLevel > BuildingDefinitions.MaxBuildingLevel)
                return BadRequest($"Building is already at max level ({BuildingDefinitions.MaxBuildingLevel}).");

            var player = await _db.Players.FindAsync(settlement.PlayerId);
            int queueLimit = player?.GetBuildQueueLimit() ?? 2;
            int activeCount = settlement.Buildings.Count(b => b.IsConstructing);

            int totalInQueue = activeCount + await _db.BuildingUpgradeQueueItems
                .CountAsync(q => q.SettlementId == settlement.Id && !q.IsStarted);
            if (totalInQueue >= queueLimit)
                return BadRequest($"Build queue is full ({totalInQueue}/{queueLimit}).");

            var cost = BuildingDefinitions.GetUpgradeCost(buildingType, targetLevel);
            if (!settlement.Resources.HasEnough(cost.Water, cost.Food, cost.Scrap, cost.Fuel, cost.Energy, cost.RareTech))
                return BadRequest("Not enough resources.");

            // Deduct resources immediately to reserve for either immediate start or queued item
            settlement.Resources.Spend(cost.Water, cost.Food, cost.Scrap, cost.Fuel, cost.Energy, cost.RareTech);

            // Create a queue item snapshot and always persist it (even when starting immediately)
            var queueItem = new BuildingUpgradeQueueItem(
                settlement.Id,
                buildingType,
                targetLevel,
                cost.Water,
                cost.Food,
                cost.Scrap,
                cost.Fuel,
                cost.Energy,
                cost.RareTech
            );

            _db.BuildingUpgradeQueueItems.Add(queueItem);

            // If there's an available active slot and this building is not currently constructing, start immediately
            if (activeCount < 1 && !building.IsConstructing)
            {
                int buildTime = BuildingDefinitions.GetBuildTimeSeconds(buildingType, targetLevel);
                // mark queue item as started and link to the building
                queueItem.MarkStarted(buildTime, building.Id);
                // start building to explicit target level
                building.StartUpgradeToLevel(targetLevel, buildTime);

                settlement.RecalculateUsedPopulation();
                await _db.SaveChangesAsync();
                await RecalculatePlayerScore(settlement.PlayerId);

                return Ok(new
                {
                    building.Id,
                    Type = building.Type.ToString(),
                    building.Level,
                    building.TargetLevel,
                    building.IsConstructing,
                    building.ConstructionEndUtc,
                    RemainingSeconds = building.GetRemainingSeconds(),
                    QueueUsed = activeCount + 1,
                    QueueLimit = queueLimit,
                    UsedPopulation = settlement.UsedPopulation,
                    AvailablePopulation = settlement.AvailablePopulation,
                    Message = $"{BuildingDefinitions.GetDisplayName(buildingType)} upgrade to level {targetLevel} started! ({activeCount + 1}/{queueLimit})"
                });
            }

            settlement.RecalculateUsedPopulation();
            await _db.SaveChangesAsync();
            await RecalculatePlayerScore(settlement.PlayerId);

            return Ok(new
            {
                QueueItem = new { queueItem.Id, Type = buildingType.ToString(), queueItem.TargetLevel, queueItem.CreatedAtUtc },
                QueueUsed = activeCount,
                QueueLimit = queueLimit,
                UsedPopulation = settlement.UsedPopulation,
                AvailablePopulation = settlement.AvailablePopulation,
                Message = $"{BuildingDefinitions.GetDisplayName(buildingType)} upgrade to level {targetLevel} queued."
            });
        }

        [HttpPost("{id}/buildings/cancel")]
        public async Task<IActionResult> CancelConstruction(Guid id, [FromBody] UpgradeBuildingRequest request)
        {
            if (!Enum.TryParse<BuildingType>(request.BuildingType, out var buildingType))
                return BadRequest($"Unknown building type: {request.BuildingType}");

            var settlement = await _db.Settlements
                .Include(s => s.Buildings)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (settlement == null)
                return NotFound("Settlement not found.");

            var building = settlement.Buildings.FirstOrDefault(b => b.Type == buildingType);

            // Load waiting items for this settlement/building
            var waitingForBuilding = await _db.BuildingUpgradeQueueItems
                .Where(q => q.SettlementId == settlement.Id && q.BuildingType == buildingType && !q.IsStarted)
                .OrderByDescending(q => q.TargetLevel)
                .ToListAsync();

            // Enforce LIFO: if a specific level is requested, it must be the highest waiting level
            if (request.TargetLevel.HasValue && waitingForBuilding.Any())
            {
                int highestWaiting = waitingForBuilding.Max(q => q.TargetLevel);
                if (request.TargetLevel.Value != highestWaiting)
                    return BadRequest(
                        $"Cannot cancel level {request.TargetLevel.Value}. " +
                        $"You must cancel level {highestWaiting} first."
                    );
            }

            // If there are waiting items, cancel the top-most (LIFO) by default
            var topQueued = waitingForBuilding.FirstOrDefault();
            if (topQueued != null)
            {
                // Refund 75% of the snapshot cost
                settlement.Resources.Add(
                    water: (int)Math.Ceiling(topQueued.CostWater * 0.75),
                    food: (int)Math.Ceiling(topQueued.CostFood * 0.75),
                    scrap: (int)Math.Ceiling(topQueued.CostScrap * 0.75),
                    fuel: (int)Math.Ceiling(topQueued.CostFuel * 0.75),
                    energy: (int)Math.Ceiling(topQueued.CostEnergy * 0.75),
                    rareTech: (int)Math.Ceiling(topQueued.CostRareTech * 0.75)
                );

                _db.BuildingUpgradeQueueItems.Remove(topQueued);

                settlement.RecalculateUsedPopulation();

                settlement.Resources.CapToStorage(
                    settlement.WaterCapacity,
                    settlement.FoodCapacity,
                    settlement.ScrapCapacity,
                    settlement.FuelCapacity,
                    settlement.EnergyCapacity,
                    settlement.RareTechCapacity
                );

                await _db.SaveChangesAsync();
                await RecalculatePlayerScore(settlement.PlayerId);

                // After removing a waiting item, try to start next queued upgrades
                await StartNextQueuedConstructionsInternal(settlement);

                var activeCountAfter = settlement.Buildings.Count(b => b.IsConstructing);
                var playerAfter = await _db.Players.FindAsync(settlement.PlayerId);
                int queueLimitAfter = playerAfter?.GetBuildQueueLimit() ?? 2;

                return Ok(new
                {
                    Message = "Queued upgrade cancelled. 75% resources refunded.",
                    QueueUsed = activeCountAfter,
                    QueueLimit = queueLimitAfter,
                    UsedPopulation = settlement.UsedPopulation,
                    AvailablePopulation = settlement.AvailablePopulation
                });
            }

            // No queued items, attempt to cancel active construction
            if (building == null || !building.IsConstructing)
                return BadRequest("No active construction or queued items for this building type.");

            // If there exist any waiting items for this building with a higher target level, disallow cancelling the active one
            var anyHigherWaiting = await _db.BuildingUpgradeQueueItems
                .AnyAsync(q => q.SettlementId == settlement.Id && q.BuildingType == buildingType && !q.IsStarted && q.TargetLevel > building.TargetLevel);
            if (anyHigherWaiting)
            {
                return BadRequest("Cannot cancel active construction while higher queued upgrades exist. Cancel queued upgrades first.");
            }

            // Enforce LIFO: if a specific level is requested, verify no higher waiting items exist
            if (request.TargetLevel.HasValue)
            {
                var highestQueued = await _db.BuildingUpgradeQueueItems
                    .Where(q => q.SettlementId == settlement.Id
                        && q.BuildingType == buildingType
                        && !q.IsStarted)
                    .Select(q => (int?)q.TargetLevel)
                    .MaxAsync();

                if (highestQueued.HasValue && highestQueued.Value > request.TargetLevel.Value)
                    return BadRequest(
                        $"Cannot cancel level {request.TargetLevel.Value}. " +
                        $"You must cancel level {highestQueued.Value} first."
                    );
            }

            // Try to find a started queue item linked to this active building (prefer ActiveBuildingId)
            var startedQueueItem = await _db.BuildingUpgradeQueueItems
                .FirstOrDefaultAsync(q => q.SettlementId == settlement.Id && q.IsStarted && q.ActiveBuildingId == building.Id);

            if (startedQueueItem == null)
            {
                // fallback: match by target level
                startedQueueItem = await _db.BuildingUpgradeQueueItems
                    .FirstOrDefaultAsync(q => q.SettlementId == settlement.Id && q.IsStarted && q.BuildingType == buildingType && q.TargetLevel == building.TargetLevel);
            }

            if (startedQueueItem == null)
            {
                // Missing snapshot for active construction: treat as inconsistency and refuse to cancel here.
                return BadRequest("Inconsistent state: missing queue snapshot for active construction. Cancel queued items first or contact support.");
            }

            settlement.Resources.Add(
                water: (int)Math.Ceiling(startedQueueItem.CostWater * 0.75),
                food: (int)Math.Ceiling(startedQueueItem.CostFood * 0.75),
                scrap: (int)Math.Ceiling(startedQueueItem.CostScrap * 0.75),
                fuel: (int)Math.Ceiling(startedQueueItem.CostFuel * 0.75),
                energy: (int)Math.Ceiling(startedQueueItem.CostEnergy * 0.75),
                rareTech: (int)Math.Ceiling(startedQueueItem.CostRareTech * 0.75)
            );

            _db.BuildingUpgradeQueueItems.Remove(startedQueueItem);

            building.CancelConstruction();

            settlement.RecalculateUsedPopulation();

            settlement.Resources.CapToStorage(
                settlement.WaterCapacity,
                settlement.FoodCapacity,
                settlement.ScrapCapacity,
                settlement.FuelCapacity,
                settlement.EnergyCapacity,
                settlement.RareTechCapacity
            );

            await _db.SaveChangesAsync();
            await RecalculatePlayerScore(settlement.PlayerId);

            // After cancelling active, try to start next waiting items
            await StartNextQueuedConstructionsInternal(settlement);

            var activeCount = settlement.Buildings.Count(b => b.IsConstructing);
            var player = await _db.Players.FindAsync(settlement.PlayerId);
            int queueLimit = player?.GetBuildQueueLimit() ?? 2;

            return Ok(new
            {
                Message = "Active construction cancelled. 75% resources refunded.",
                QueueUsed = activeCount,
                QueueLimit = queueLimit,
                UsedPopulation = settlement.UsedPopulation,
                AvailablePopulation = settlement.AvailablePopulation
            });
        }

        [HttpPost("{id}/buildings/instant-finish")]
        public async Task<IActionResult> InstantFinishBuilding(Guid id)
        {
            const int thresholdSeconds = 300; // 5 minutes

            var settlement = await _db.Settlements
                .Include(s => s.Buildings)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (settlement == null)
                return NotFound("Settlement not found.");

            var building = settlement.Buildings
                .Where(b => b.IsConstructing && b.GetRemainingSeconds() <= thresholdSeconds)
                .OrderBy(b => b.ConstructionEndUtc)
                .FirstOrDefault();

            if (building == null)
                return BadRequest("No building eligible for instant finish (requires \u2264 5 minutes remaining).");

            building.ForceComplete();
            settlement.RecalculateUsedPopulation();
            await _db.SaveChangesAsync();

            await StartNextQueuedConstructionsInternal(settlement);
            await RecalculatePlayerScore(settlement.PlayerId);

            return Ok(new
            {
                Message = "Construction completed instantly.",
                BuildingType = building.Type.ToString(),
                building.Level
            });
        }

        [HttpPost("{id}/commander/activate")]
        public async Task<IActionResult> ActivateCommander(Guid id, [FromBody] ActivateCommanderRequest? request)
        {
            var settlement = await _db.Settlements.FindAsync(id);
            if (settlement == null)
                return NotFound("Settlement not found.");

            var player = await _db.Players.FindAsync(settlement.PlayerId);
            if (player == null)
                return NotFound("Player not found.");

            int days = request?.Days ?? 7;
            if (days <= 0)
                player.ActivateCommanderPermanent();
            else
                player.ActivateCommander(TimeSpan.FromDays(days));

            await _db.SaveChangesAsync();

            return Ok(new
            {
                Message = $"Commander activated! Build queue expanded to {player.GetBuildQueueLimit()} slots.",
                CommanderActive = player.CommanderActive,
                CommanderExpiresUtc = player.CommanderExpiresUtc,
                QueueLimit = player.GetBuildQueueLimit()
            });
        }

        [HttpPost("{id}/commander/deactivate")]
        public async Task<IActionResult> DeactivateCommander(Guid id)
        {
            var settlement = await _db.Settlements.FindAsync(id);
            if (settlement == null)
                return NotFound("Settlement not found.");

            var player = await _db.Players.FindAsync(settlement.PlayerId);
            if (player == null)
                return NotFound("Player not found.");

            player.DeactivateCommander();
            await _db.SaveChangesAsync();

            return Ok(new
            {
                Message = "Commander deactivated.",
                QueueLimit = player.GetBuildQueueLimit()
            });
        }

        [HttpGet("world/{seed}")]
        public async Task<IActionResult> GetWorldSettlements(int seed)
        {
            var allSettlements = await _db.Settlements
                .Include(s => s.Buildings)
                .OrderBy(s => s.Id)
                .ToListAsync();

            var playerIds = allSettlements.Select(s => s.PlayerId).Distinct().ToList();
            var players = await _db.Players
                .Where(p => playerIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            Guid currentPlayerId = Guid.Empty;
            if (Request.Headers.TryGetValue("X-Player-Id", out var playerIdHeader))
                Guid.TryParse(playerIdHeader, out currentPlayerId);

            var myRelations = new Dictionary<Guid, string>();
            if (currentPlayerId != Guid.Empty)
            {
                var relations = await _db.PlayerRelations
                    .Where(r => r.PlayerId == currentPlayerId)
                    .ToListAsync();

                foreach (var r in relations)
                    myRelations[r.TargetPlayerId] = r.Type.ToString().ToLower();

                // Alliance members are always shown as ally on the map
                var myMembership = await _db.AllianceMembers
                    .FirstOrDefaultAsync(m => m.PlayerId == currentPlayerId);

                if (myMembership != null)
                {
                    var allianceMembers = await _db.AllianceMembers
                        .Where(m => m.AllianceId == myMembership.AllianceId && m.PlayerId != currentPlayerId)
                        .Select(m => m.PlayerId)
                        .ToListAsync();

                    foreach (var memberId in allianceMembers)
                        myRelations[memberId] = "ally";
                }
            }

            var result = allSettlements.Select((s, index) =>
            {
                players.TryGetValue(s.PlayerId, out var player);
                int defense = s.Buildings.Sum(b => BuildingDefinitions.GetDefenseValue(b.Type, b.Level));
                int power = s.Buildings.Sum(b => BuildingDefinitions.GetPowerValue(b.Type, b.Level));

                string status = s.PlayerId == currentPlayerId
                    ? "yours"
                    : myRelations.TryGetValue(s.PlayerId, out var rel)
                        ? rel
                        : "neutral";

                return new
                {
                    s.Id,
                    s.Name,
                    s.PlayerId,
                    PlayerName = player?.Username ?? "Unknown",
                    SlotIndex = index,
                    Score = player?.Score ?? 0,
                    Defense = defense,
                    Power = power,
                    s.Morale,
                    Status = status,
                    IsOwn = s.PlayerId == currentPlayerId,
                    BuildingCount = s.Buildings.Count(b => b.Level > 0),
                };
            });

            return Ok(result);
        }

        [HttpGet("{id}/units/queue")]
        public async Task<IActionResult> GetUnitTrainingQueue(Guid id)
        {
            var settlement = await _db.Settlements
                .Include(s => s.TrainingQueue)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (settlement == null)
                return NotFound("Settlement not found.");

            CompleteReadyUnitTrainingInternal(settlement);
            settlement.RecalculateUsedPopulation();
            await _db.SaveChangesAsync();

            var player = await _db.Players.FindAsync(settlement.PlayerId);
            int queueLimit = player?.GetBuildQueueLimit() ?? 2;

            var now2 = DateTime.UtcNow;
            var activeQueue = settlement.TrainingQueue
                .Where(q => !q.IsCompleted)
                .OrderBy(q => q.EndsAtUtc)
                .Select(q =>
                {
                    double totalSec = (q.EndsAtUtc - q.StartedAtUtc).TotalSeconds;
                    int perUnit = Math.Max(1, (int)Math.Round(totalSec / q.Quantity));
                    return new
                    {
                        q.Id,
                        q.UnitName,
                        q.Quantity,
                        q.DeliveredQuantity,
                        q.StartedAtUtc,
                        q.EndsAtUtc,
                        q.PopulationCostPerUnit,
                        PerUnitDurationSeconds = perUnit,
                        RemainingSeconds = q.GetRemainingSeconds(),
                        TotalRemainingSeconds = q.GetTotalRemainingSeconds()
                    };
                })
                .ToList();

            return Ok(new
            {
                Queue = activeQueue,
                Active = activeQueue.Count,
                Limit = queueLimit,
                SlotsAvailable = Math.Max(0, queueLimit - activeQueue.Count),
                CommanderActive = player?.CommanderActive ?? false,
                CommanderExpiresUtc = player?.CommanderExpiresUtc
            });
        }

        [HttpPost("{id}/units/complete-ready")]
        public async Task<IActionResult> CompleteReadyUnitTraining(Guid id)
        {
            var settlement = await _db.Settlements
                .Include(s => s.TrainingQueue)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (settlement == null)
                return NotFound("Settlement not found.");

            int completed = CompleteReadyUnitTrainingInternal(settlement);
            settlement.RecalculateUsedPopulation();

            await _db.SaveChangesAsync();

            return Ok(new
            {
                CompletedCount = completed,
                UnitInventory = settlement.UnitInventory,
                UsedPopulation = settlement.UsedPopulation,
                PopulationCapacity = settlement.PopulationCapacity,
                AvailablePopulation = settlement.AvailablePopulation
            });
        }

        [HttpPost("{id}/units/train")]
        public async Task<IActionResult> TrainUnit(Guid id, [FromBody] TrainUnitRequest request)
        {
            var settlement = await _db.Settlements
                .Include(s => s.Buildings)
                .Include(s => s.TrainingQueue)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (settlement == null)
                return NotFound("Settlement not found.");

            if (request.Quantity <= 0)
                return BadRequest("Quantity must be greater than zero.");

            CompleteReadyUnitTrainingInternal(settlement);
            settlement.RecalculateUsedPopulation();

            var allUnits = UnitFactory.CreateStarterUnits();
            var unit = allUnits.FirstOrDefault(u => u.Name.Equals(request.UnitName, StringComparison.OrdinalIgnoreCase));

            if (unit == null)
                return BadRequest($"Unknown unit: {request.UnitName}");

            bool facilityUnlocked = unit.Facility switch
            {
                ProductionFacility.Barracks => settlement.Buildings.Any(b => b.Type == BuildingType.Barracks && b.Level > 0),
                ProductionFacility.Garage => settlement.Buildings.Any(b => b.Type == BuildingType.Garage && b.Level > 0),
                ProductionFacility.Workshop => settlement.Buildings.Any(b => b.Type == BuildingType.Workshop && b.Level > 0),
                ProductionFacility.CommandCenter => settlement.Buildings.Any(b => b.Type == BuildingType.CommandCenter && b.Level > 0),
                _ => false
            };

            if (!facilityUnlocked)
                return BadRequest($"Required facility not unlocked for {unit.Name}.");

            var player = await _db.Players.FindAsync(settlement.PlayerId);
            int queueLimit = player?.GetBuildQueueLimit() ?? 2;
            int activeQueueCount = settlement.TrainingQueue.Count(q => !q.IsCompleted);

            if (activeQueueCount >= queueLimit)
                return BadRequest($"Training queue full ({activeQueueCount}/{queueLimit}).");

            var totalCost = new
            {
                Water = unit.Cost.Water * request.Quantity,
                Food = unit.Cost.Food * request.Quantity,
                Scrap = unit.Cost.Scrap * request.Quantity,
                Fuel = unit.Cost.Fuel * request.Quantity,
                Energy = unit.Cost.Energy * request.Quantity,
                RareTech = unit.Cost.RareTech * request.Quantity
            };

            if (!settlement.Resources.HasEnough(
                totalCost.Water,
                totalCost.Food,
                totalCost.Scrap,
                totalCost.Fuel,
                totalCost.Energy,
                totalCost.RareTech))
            {
                return BadRequest("Not enough resources.");
            }

            int requiredPopulation = unit.CapacityCost * request.Quantity;
            if (settlement.AvailablePopulation < requiredPopulation)
            {
                return BadRequest($"Not enough population. Required: {requiredPopulation}, Available: {settlement.AvailablePopulation}");
            }

            settlement.Resources.Spend(
                totalCost.Water,
                totalCost.Food,
                totalCost.Scrap,
                totalCost.Fuel,
                totalCost.Energy,
                totalCost.RareTech
            );

            int durationSeconds = Math.Max(1, unit.BuildTimeSeconds * request.Quantity);
            var queueItem = settlement.QueueUnitTraining(
                unit.Name,
                request.Quantity,
                unit.CapacityCost,
                durationSeconds
            );

            _db.UnitTrainingQueueItems.Add(queueItem);

            settlement.RecalculateUsedPopulation();

            await _db.SaveChangesAsync();

            return Ok(new
            {
                Message = $"{request.Quantity}x {unit.Name} added to training queue.",
                QueueItem = new
                {
                    queueItem.Id,
                    queueItem.UnitName,
                    queueItem.Quantity,
                    queueItem.StartedAtUtc,
                    queueItem.EndsAtUtc,
                    RemainingSeconds = queueItem.GetRemainingSeconds()
                },
                QueueUsed = activeQueueCount + 1,
                QueueLimit = queueLimit,
                UsedPopulation = settlement.UsedPopulation,
                PopulationCapacity = settlement.PopulationCapacity,
                AvailablePopulation = settlement.AvailablePopulation
            });
        }

        private int CompleteReadyUnitTrainingInternal(Settlement settlement)
        {
            if (settlement.TrainingQueue == null || settlement.TrainingQueue.Count == 0)
                return 0;

            var now = DateTime.UtcNow;
            int totalDelivered = 0;

            foreach (var item in settlement.TrainingQueue.Where(q => !q.IsCompleted))
            {
                double totalSeconds = (item.EndsAtUtc - item.StartedAtUtc).TotalSeconds;
                int perUnit = Math.Max(1, (int)Math.Round(totalSeconds / item.Quantity));
                double elapsed = (now - item.StartedAtUtc).TotalSeconds;
                int unitsReady = Math.Min(item.Quantity, (int)(elapsed / perUnit));

                int toDeliver = unitsReady - item.DeliveredQuantity;
                if (toDeliver > 0)
                {
                    settlement.AddUnits(item.UnitName, toDeliver, item.PopulationCostPerUnit);
                    item.DeliverUnits(toDeliver);
                    totalDelivered += toDeliver;
                }

                if (item.DeliveredQuantity >= item.Quantity || now >= item.EndsAtUtc)
                    item.MarkCompleted();
            }

            return totalDelivered;
        }

        private async Task StartNextQueuedConstructionsInternal(Settlement settlement)
        {
            if (settlement == null) return;

            var player = await _db.Players.FindAsync(settlement.PlayerId);
            int queueLimit = player?.GetBuildQueueLimit() ?? 2;
            int activeCount = settlement.Buildings.Count(b => b.IsConstructing);

            if (activeCount >= 1)
                return;

            // First, remove any started queue items that are linked to buildings which are no longer constructing.
            // Use ExecuteDeleteAsync (direct SQL) instead of Remove() + SaveChangesAsync() to avoid
            // DbUpdateConcurrencyException when concurrent requests both attempt to delete the same rows.
            var startedItems = await _db.BuildingUpgradeQueueItems
                .Where(q => q.SettlementId == settlement.Id && q.IsStarted && q.ActiveBuildingId != null)
                .ToListAsync();

            var staleIds = startedItems
                .Where(si =>
                {
                    var linked = settlement.Buildings.FirstOrDefault(b => b.Id == si.ActiveBuildingId);
                    return linked == null || !linked.IsConstructing;
                })
                .Select(si => si.Id)
                .ToList();

            if (staleIds.Count > 0)
            {
                await _db.BuildingUpgradeQueueItems
                    .Where(q => staleIds.Contains(q.Id))
                    .ExecuteDeleteAsync();
            }

            var waiting = await _db.BuildingUpgradeQueueItems
                .Where(q => q.SettlementId == settlement.Id && !q.IsStarted)
                .OrderBy(q => q.CreatedAtUtc)
                .ToListAsync();

            foreach (var q in waiting)
            {
                if (activeCount >= 1)
                    break;

                // ensure this is the next waiting item for its building (no lower-target waiting remains)
                bool hasLowerWaiting = waiting.Any(x => x.BuildingType == q.BuildingType && x.TargetLevel < q.TargetLevel && !x.IsStarted);
                if (hasLowerWaiting)
                    continue;

                var building = settlement.Buildings.FirstOrDefault(b => b.Type == q.BuildingType);
                if (building == null)
                {
                    int startingLevel = BuildingDefinitions.GetStartingLevel(q.BuildingType);
                    building = startingLevel > 0
                        ? Building.CreateAtLevel(settlement.Id, q.BuildingType, startingLevel)
                        : new Building(settlement.Id, q.BuildingType);
                    _db.Buildings.Add(building);
                    settlement.Buildings.Add(building);
                }

                building.TryCompleteConstruction();
                if (building.IsConstructing)
                    continue;

                // Guard: remove stale queue items whose target level the building has already reached or surpassed
                if (building.Level >= q.TargetLevel)
                {
                    _db.BuildingUpgradeQueueItems.Remove(q);
                    continue;
                }

                int buildTime = BuildingDefinitions.GetBuildTimeSeconds(q.BuildingType, q.TargetLevel);
                // mark queue item as started and link to the building
                q.MarkStarted(buildTime, building.Id);
                // start building to explicit target level
                building.StartUpgradeToLevel(q.TargetLevel, buildTime);

                activeCount++;
            }

            await _db.SaveChangesAsync();
            await RecalculatePlayerScore(settlement.PlayerId);
        }

        private async Task RecalculatePlayerScore(Guid playerId)
        {
            var player = await _db.Players.FindAsync(playerId);
            if (player == null) return;

            var settlements = await _db.Settlements
                .Include(s => s.Buildings)
                .Where(s => s.PlayerId == playerId)
                .ToListAsync();

            int totalPower = settlements.Sum(s => s.GetTotalBuildingPower());
            player.SetScore(totalPower);
            await _db.SaveChangesAsync();
        }

        private void EnsureStarterBuildings(Settlement settlement)
        {
            foreach (var kvp in BuildingDefinitions.StarterBuildingLevels)
            {
                var type = kvp.Key;
                var level = kvp.Value;

                if (!settlement.Buildings.Any(b => b.Type == type))
                {
                    var newBuilding = Building.CreateAtLevel(settlement.Id, type, level);
                    settlement.Buildings.Add(newBuilding);
                    _db.Buildings.Add(newBuilding);
                }
            }
        }

        private static Dictionary<BuildingType, int> GetCurrentLevels(Settlement settlement)
        {
            var result = Enum.GetValues<BuildingType>()
                .ToDictionary(type => type, type => 0);

            foreach (var building in settlement.Buildings)
            {
                if (result[building.Type] < building.Level)
                    result[building.Type] = building.Level;
            }

            foreach (var starter in BuildingDefinitions.StarterBuildingLevels)
            {
                if (result[starter.Key] < starter.Value)
                    result[starter.Key] = starter.Value;
            }

            return result;
        }

        private static object MapBuilding(Building b, Dictionary<BuildingType, int> currentLevels, bool queueFull)
        {
            int level = b.Level;
            int nextLevel = level + 1;
            bool prerequisitesMet = BuildingDefinitions.ArePrerequisitesMet(b.Type, currentLevels);
            var prereqs = BuildingDefinitions.GetPrerequisites(b.Type);
            bool isFutureFeature = BuildingDefinitions.IsFutureFeature(b.Type);
            bool isBuildable = BuildingDefinitions.IsBuildable(b.Type);

            return new
            {
                b.Id,
                Type = b.Type.ToString(),
                DisplayName = BuildingDefinitions.GetDisplayName(b.Type),
                Description = BuildingDefinitions.GetDescription(b.Type),
                Category = BuildingDefinitions.GetCategory(b.Type),

                StartingLevel = BuildingDefinitions.GetStartingLevel(b.Type),
                Level = b.Level,
                EffectiveLevel = level,

                IsConstructing = b.IsConstructing,
                TargetLevel = b.TargetLevel,
                RemainingSeconds = b.GetRemainingSeconds(),
                ConstructionEndUtc = b.ConstructionEndUtc,

                IsFutureFeature = isFutureFeature,
                IsBuildable = isBuildable,
                IsLocked = (!prerequisitesMet && b.Level == 0) || isFutureFeature,

                Prerequisites = prereqs?.Select(p => new
                {
                    Type = p.RequiredType.ToString(),
                    DisplayName = BuildingDefinitions.GetDisplayName(p.RequiredType),
                    RequiredLevel = p.RequiredLevel,
                    CurrentLevel = currentLevels.GetValueOrDefault(p.RequiredType, 0),
                    IsMet = currentLevels.GetValueOrDefault(p.RequiredType, 0) >= p.RequiredLevel
                }),

                CanUpgrade = isBuildable
                    && prerequisitesMet
                    && nextLevel <= BuildingDefinitions.MaxBuildingLevel
                    && !b.IsConstructing
                    && !queueFull,

                QueueFull = queueFull && !b.IsConstructing,

                UpgradeCost = isBuildable && nextLevel <= BuildingDefinitions.MaxBuildingLevel
                    ? BuildingDefinitions.GetUpgradeCost(b.Type, nextLevel)
                    : null,

                BuildTimeSeconds = isBuildable && nextLevel <= BuildingDefinitions.MaxBuildingLevel
                    ? BuildingDefinitions.GetBuildTimeSeconds(b.Type, nextLevel)
                    : 0,

                HourlyProduction = BuildingDefinitions.GetHourlyProduction(b.Type, b.Level),
                PopulationUsage = BuildingDefinitions.GetPopulationUsage(b.Type, b.Level),
                PowerValue = BuildingDefinitions.GetPowerValue(b.Type, b.Level),
                StorageBonus = BuildingDefinitions.GetStorageBonus(b.Type, b.Level),
                DefenseValue = BuildingDefinitions.GetDefenseValue(b.Type, b.Level)
            };
        }

        // ── Salvage inventory ──────────────────────────────────────────────
        [HttpGet("{id}/salvage")]
        public async Task<IActionResult> GetSalvageInventory(Guid id)
        {
            var inv = await _db.SettlementSalvageInventories
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.SettlementId == id);

            if (inv == null)
                return Ok(new { items = Array.Empty<object>(), storedRareTech = 0 });

            return Ok(new
            {
                items = inv.GetAllItems().Select(i => new
                {
                    i.Key,
                    i.Name,
                    i.Description,
                    i.SourceType,
                    i.Rarity,
                    i.Quantity,
                    i.RequiredTechSalvagerLevel,
                    i.BaseSalvageTimeSeconds,
                    i.RareTechYield,
                    i.ResearchDataYield,
                    i.SpecialOutputKey,
                    i.AcquiredAtUtc
                }),
                storedRareTech = inv.StoredRareTech
            });
        }

        [HttpPost("{id}/salvage/process")]
        public async Task<IActionResult> ProcessSalvageItem(Guid id, [FromBody] ProcessSalvageRequest request)
        {
            var settlement = await _db.Settlements
                .Include(s => s.Buildings)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (settlement == null) return NotFound("Settlement not found.");

            var inv = await _db.SettlementSalvageInventories
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.SettlementId == id);
            if (inv == null) return NotFound("No salvage inventory found for this settlement.");

            var item = inv.GetByKey(request.ItemKey);
            if (item == null) return BadRequest("Salvage item not found.");

            int qty = Math.Max(1, request.Quantity);
            if (item.Quantity < qty) return BadRequest($"Not enough quantity. Have {item.Quantity}, requested {qty}.");

            int rareTechGained = item.RareTechYield * qty;
            int researchDataGained = item.ResearchDataYield * qty;

            inv.RemoveItem(request.ItemKey, qty);

            if (rareTechGained > 0)
            {
                settlement.AddResourcesCapped(0, 0, 0, 0, 0, rareTechGained);
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                rareTechGained,
                researchDataGained,
                message = $"Processed {qty}x {item.Name}. Gained {rareTechGained} RT."
            });
        }
    }

    public class ProcessSalvageRequest
    {
        public string ItemKey { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
    }

    public class UpgradeBuildingRequest
    {
        public string BuildingType { get; set; } = string.Empty;
        public int? TargetLevel { get; set; }
    }

    public class ActivateCommanderRequest
    {
        public int Days { get; set; } = 7;
    }

    public class TrainUnitRequest
    {
        public string UnitName { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
    }

    public class DepositRequest
    {
        public int Amount { get; set; }
    }
}