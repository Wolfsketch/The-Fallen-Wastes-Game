using System;
using System.Linq;
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
    public class ResearchController : ControllerBase
    {
        private readonly GameDbContext _db;

        public ResearchController(GameDbContext db)
        {
            _db = db;
        }

        [HttpGet("{settlementId}")]
        public async Task<IActionResult> GetResearchState(Guid settlementId)
        {
            var settlement = await _db.Settlements
                .Include(s => s.Buildings)
                .FirstOrDefaultAsync(s => s.Id == settlementId);

            if (settlement == null)
                return NotFound("Settlement not found.");

            var state = await GetOrCreateResearchState(settlementId);

            state.CompleteReadyResearches(DateTime.UtcNow);
            await _db.SaveChangesAsync();

            int techLabLevel = settlement.Buildings
                .Where(b => b.Type == BuildingType.TechLab)
                .Select(b => b.Level)
                .DefaultIfEmpty(0)
                .Max();

            var definitions = ResearchDefinitions.GetAll();

            var result = definitions.Select(def =>
            {
                var existing = state.GetResearch(def.Key);
                bool isUnlocked = existing?.IsUnlocked ?? false;
                bool isResearching = existing?.IsResearching ?? false;
                int remainingSeconds = existing?.GetRemainingSeconds(DateTime.UtcNow) ?? 0;

                bool techLabMet = techLabLevel >= def.RequiredTechLabLevel;
                bool prereqResearchMet = def.RequiredResearchKeys.All(state.HasUnlocked);

                return new
                {
                    key = def.Key,
                    name = def.Name,
                    description = def.Description,
                    branch = def.Branch,

                    requiredTechLabLevel = def.RequiredTechLabLevel,
                    rareTechCost = def.RareTechCost,
                    researchPointCost = def.ResearchPointCost,
                    baseDurationSeconds = def.BaseDurationSeconds,

                    requiredResearchKeys = def.RequiredResearchKeys,
                    requiredSalvageItems = def.RequiredSalvageItems,
                    isFutureFeature = def.IsFutureFeature,

                    isUnlocked,
                    isResearching,
                    remainingSeconds,

                    requirements = new
                    {
                        techLabLevel,
                        techLabMet,
                        prerequisiteResearchMet = prereqResearchMet
                    },

                    canStart =
                        !def.IsFutureFeature &&
                        !isUnlocked &&
                        !isResearching &&
                        techLabMet &&
                        prereqResearchMet &&
                        state.CanStartAnotherResearch() &&
                        settlement.Resources.RareTech >= def.RareTechCost &&
                        state.ResearchPoints >= def.ResearchPointCost
                };
            });

            return Ok(new
            {
                settlementId,
                techLabLevel,
                researchPoints = state.ResearchPoints,
                maxConcurrentResearches = state.MaxConcurrentResearches,
                availableResearchSlots = state.GetAvailableResearchSlots(),
                researches = result
            });
        }

        [HttpPost("{settlementId}/start")]
        public async Task<IActionResult> StartResearch(Guid settlementId, [FromBody] StartResearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ResearchKey))
                return BadRequest("ResearchKey is required.");

            var settlement = await _db.Settlements
                .Include(s => s.Buildings)
                .FirstOrDefaultAsync(s => s.Id == settlementId);

            if (settlement == null)
                return NotFound("Settlement not found.");

            var state = await GetOrCreateResearchState(settlementId);

            state.CompleteReadyResearches(DateTime.UtcNow);

            if (!state.CanStartAnotherResearch())
                return BadRequest("No research slots available.");

            var definition = ResearchDefinitions.GetByKey(request.ResearchKey);
            if (definition == null)
                return BadRequest($"Unknown research key: {request.ResearchKey}");

            if (definition.IsFutureFeature)
                return BadRequest("This research is not available yet.");

            var existing = state.GetResearch(definition.Key);
            if (existing?.IsUnlocked == true)
                return BadRequest("This research is already unlocked.");

            if (existing?.IsResearching == true)
                return BadRequest("This research is already in progress.");

            int techLabLevel = settlement.Buildings
                .Where(b => b.Type == BuildingType.TechLab)
                .Select(b => b.Level)
                .DefaultIfEmpty(0)
                .Max();

            if (techLabLevel < definition.RequiredTechLabLevel)
                return BadRequest($"Tech Lab level {definition.RequiredTechLabLevel} required.");

            foreach (var prereqKey in definition.RequiredResearchKeys)
            {
                if (!state.HasUnlocked(prereqKey))
                    return BadRequest($"Missing prerequisite research: {prereqKey}");
            }

            if (settlement.Resources.RareTech < definition.RareTechCost)
                return BadRequest("Not enough RareTech.");

            if (state.ResearchPoints < definition.ResearchPointCost)
                return BadRequest("Not enough Research Points.");

            settlement.Resources.Spend(
                water: 0,
                food: 0,
                scrap: 0,
                fuel: 0,
                energy: 0,
                rareTech: definition.RareTechCost
            );

            var research = existing ?? new Research(
                settlementId: settlementId,
                key: definition.Key,
                name: definition.Name,
                description: definition.Description,
                branch: definition.Branch,
                requiredTechLabLevel: definition.RequiredTechLabLevel,
                rareTechCost: definition.RareTechCost,
                researchPointCost: definition.ResearchPointCost,
                baseDurationSeconds: definition.BaseDurationSeconds
            );

            if (existing == null)
            {
                state.AddResearch(research);
                _db.Researches.Add(research);
            }

            int durationSeconds = GetEffectiveResearchDuration(definition.BaseDurationSeconds, techLabLevel);
            research.Start(DateTime.UtcNow, durationSeconds);

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = $"Research '{definition.Name}' started.",
                researchKey = definition.Key,
                startedAtUtc = DateTime.UtcNow,
                durationSeconds,
                remainingSeconds = research.GetRemainingSeconds(DateTime.UtcNow)
            });
        }

        [HttpPost("{settlementId}/cancel")]
        public async Task<IActionResult> CancelResearch(Guid settlementId, [FromBody] StartResearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ResearchKey))
                return BadRequest("ResearchKey is required.");

            var state = await _db.SettlementResearchStates
                .Include(s => s.Researches)
                .FirstOrDefaultAsync(s => s.SettlementId == settlementId);

            if (state == null)
                return NotFound("Research state not found for this settlement.");

            var research = state.GetResearch(request.ResearchKey);
            if (research == null || !research.IsResearching)
                return BadRequest("Research is not currently in progress.");

            research.Cancel();
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = $"Research '{research.Name}' cancelled.",
                researchKey = research.Key
            });
        }

        [HttpPost("{settlementId}/complete-ready")]
        public async Task<IActionResult> CompleteReadyResearch(Guid settlementId)
        {
            var state = await _db.SettlementResearchStates
                .Include(s => s.Researches)
                .FirstOrDefaultAsync(s => s.SettlementId == settlementId);

            if (state == null)
                return NotFound("Research state not found for this settlement.");

            var before = state.GetUnlockedResearches().Count;
            state.CompleteReadyResearches(DateTime.UtcNow);
            var after = state.GetUnlockedResearches().Count;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Ready researches processed.",
                newlyCompleted = Math.Max(0, after - before),
                totalUnlocked = after
            });
        }

        private async Task<SettlementResearchState> GetOrCreateResearchState(Guid settlementId)
        {
            var state = await _db.SettlementResearchStates
                .Include(s => s.Researches)
                .FirstOrDefaultAsync(s => s.SettlementId == settlementId);

            if (state != null)
                return state;

            state = new SettlementResearchState(settlementId, researchPoints: 12, maxConcurrentResearches: 1);
            _db.SettlementResearchStates.Add(state);
            await _db.SaveChangesAsync();

            return state;
        }

        private static int GetEffectiveResearchDuration(int baseDurationSeconds, int techLabLevel)
        {
            if (techLabLevel <= 0)
                return baseDurationSeconds;

            double speedFactor = 1.0 + ((techLabLevel - 1) * 0.05);
            return Math.Max(60, (int)Math.Round(baseDurationSeconds / speedFactor));
        }
    }

    public class StartResearchRequest
    {
        public string ResearchKey { get; set; } = string.Empty;
    }
}