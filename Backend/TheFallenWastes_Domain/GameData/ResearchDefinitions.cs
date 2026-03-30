using System;
using System.Collections.Generic;
using System.Linq;

namespace TheFallenWastes_Domain.GameData
{
    public class ResearchDefinition
    {
        public string Key { get; }
        public string Name { get; }
        public string Description { get; }
        public string Branch { get; }

        public int RequiredTechLabLevel { get; }
        public int RareTechCost { get; }
        public int ResearchPointCost { get; }
        public int BaseDurationSeconds { get; }

        public IReadOnlyList<string> RequiredResearchKeys { get; }
        public IReadOnlyList<string> RequiredSalvageItems { get; }

        public bool IsFutureFeature { get; }

        public ResearchDefinition(
            string key,
            string name,
            string description,
            string branch,
            int requiredTechLabLevel,
            int rareTechCost,
            int researchPointCost,
            int baseDurationSeconds,
            IEnumerable<string>? requiredResearchKeys = null,
            IEnumerable<string>? requiredSalvageItems = null,
            bool isFutureFeature = false)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Research key cannot be empty.", nameof(key));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Research name cannot be empty.", nameof(name));

            if (string.IsNullOrWhiteSpace(branch))
                throw new ArgumentException("Research branch cannot be empty.", nameof(branch));

            if (requiredTechLabLevel < 0)
                throw new ArgumentException("Required Tech Lab level cannot be negative.", nameof(requiredTechLabLevel));

            if (rareTechCost < 0)
                throw new ArgumentException("RareTech cost cannot be negative.", nameof(rareTechCost));

            if (researchPointCost < 0)
                throw new ArgumentException("Research Point cost cannot be negative.", nameof(researchPointCost));

            if (baseDurationSeconds <= 0)
                throw new ArgumentException("Base duration must be greater than zero.", nameof(baseDurationSeconds));

            Key = key.Trim().ToLowerInvariant();
            Name = name.Trim();
            Description = description?.Trim() ?? string.Empty;
            Branch = branch.Trim();

            RequiredTechLabLevel = requiredTechLabLevel;
            RareTechCost = rareTechCost;
            ResearchPointCost = researchPointCost;
            BaseDurationSeconds = baseDurationSeconds;

            RequiredResearchKeys = (requiredResearchKeys ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();

            RequiredSalvageItems = (requiredSalvageItems ?? Array.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            IsFutureFeature = isFutureFeature;
        }
    }

    public static class ResearchDefinitions
    {
        private static readonly List<ResearchDefinition> _all = new()
        {
            // ECONOMY
            new ResearchDefinition(
                key: "rationing_protocols",
                name: "Rationing Protocols",
                description: "Increases the efficiency of Food consumption and internal distribution.",
                branch: "Economy",
                requiredTechLabLevel: 1,
                rareTechCost: 10,
                researchPointCost: 1,
                baseDurationSeconds: 1800
            ),
            new ResearchDefinition(
                key: "water_recycling",
                name: "Water Recycling",
                description: "Increases the efficiency of Water processing in the settlement.",
                branch: "Economy",
                requiredTechLabLevel: 2,
                rareTechCost: 14,
                researchPointCost: 1,
                baseDurationSeconds: 2700,
                requiredResearchKeys: new[] { "rationing_protocols" }
            ),
            new ResearchDefinition(
                key: "scrap_sorting",
                name: "Scrap Sorting",
                description: "Improves the yield of recovered Scrap materials.",
                branch: "Economy",
                requiredTechLabLevel: 3,
                rareTechCost: 18,
                researchPointCost: 2,
                baseDurationSeconds: 3600
            ),

            // MILITARY
            new ResearchDefinition(
                key: "improved_ballistics",
                name: "Improved Ballistics",
                description: "Enhances Ballistic weapon platforms and ammunition output.",
                branch: "Military",
                requiredTechLabLevel: 2,
                rareTechCost: 16,
                researchPointCost: 1,
                baseDurationSeconds: 3000
            ),
            new ResearchDefinition(
                key: "impact_plating",
                name: "Impact Plating",
                description: "Increases protection against heavy Impact-type attacks.",
                branch: "Military",
                requiredTechLabLevel: 3,
                rareTechCost: 22,
                researchPointCost: 2,
                baseDurationSeconds: 4200
            ),
            new ResearchDefinition(
                key: "energy_focusing",
                name: "Energy Focusing",
                description: "Improves stability and output of Energy-based weapon systems.",
                branch: "Military",
                requiredTechLabLevel: 4,
                rareTechCost: 28,
                researchPointCost: 2,
                baseDurationSeconds: 5400,
                requiredResearchKeys: new[] { "improved_ballistics" }
            ),

            // DEFENSE
            new ResearchDefinition(
                key: "fortified_barriers",
                name: "Fortified Barriers",
                description: "Reinforces the structural efficiency of Perimeter Wall systems.",
                branch: "Defense",
                requiredTechLabLevel: 3,
                rareTechCost: 24,
                researchPointCost: 2,
                baseDurationSeconds: 4200
            ),
            new ResearchDefinition(
                key: "emergency_response_drills",
                name: "Emergency Response Drills",
                description: "Improves defense response time and internal combat readiness.",
                branch: "Defense",
                requiredTechLabLevel: 4,
                rareTechCost: 26,
                researchPointCost: 2,
                baseDurationSeconds: 4800
            ),

            // LOGISTICS
            new ResearchDefinition(
                key: "convoy_protocols",
                name: "Convoy Protocols",
                description: "Unlocks and supports advanced Convoy operations.",
                branch: "Logistics",
                requiredTechLabLevel: 5,
                rareTechCost: 35,
                researchPointCost: 3,
                baseDurationSeconds: 7200,
                requiredResearchKeys: new[] { "fortified_barriers", "improved_ballistics" }
            ),
            new ResearchDefinition(
                key: "field_logistics",
                name: "Field Logistics",
                description: "Improves transport coordination and operational deployment efficiency.",
                branch: "Logistics",
                requiredTechLabLevel: 4,
                rareTechCost: 30,
                researchPointCost: 2,
                baseDurationSeconds: 5400
            ),

            // SALVAGE
            new ResearchDefinition(
                key: "salvage_optimization",
                name: "Salvage Optimization",
                description: "Increases the efficiency of the Tech Salvager during standard recovery operations.",
                branch: "Salvage",
                requiredTechLabLevel: 2,
                rareTechCost: 15,
                researchPointCost: 1,
                baseDurationSeconds: 2400
            ),
            new ResearchDefinition(
                key: "datacore_reconstruction",
                name: "Datacore Reconstruction",
                description: "Enables complex event and POI artefacts to be processed for research use.",
                branch: "Salvage",
                requiredTechLabLevel: 5,
                rareTechCost: 40,
                researchPointCost: 3,
                baseDurationSeconds: 7800,
                requiredResearchKeys: new[] { "salvage_optimization" },
                requiredSalvageItems: new[] { "Military Datacore" }
            ),

            // EXPANSION
            new ResearchDefinition(
                key: "settlement_coordination",
                name: "Settlement Coordination",
                description: "Improves administrative coordination between multiple settlements.",
                branch: "Expansion",
                requiredTechLabLevel: 6,
                rareTechCost: 45,
                researchPointCost: 3,
                baseDurationSeconds: 9000,
                requiredResearchKeys: new[] { "convoy_protocols", "field_logistics" }
            )
        };

        public static IReadOnlyList<ResearchDefinition> GetAll()
        {
            return _all;
        }

        public static ResearchDefinition? GetByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            return _all.FirstOrDefault(r => r.Key == key.Trim().ToLowerInvariant());
        }

        public static IReadOnlyList<ResearchDefinition> GetByBranch(string branch)
        {
            if (string.IsNullOrWhiteSpace(branch))
                return Array.Empty<ResearchDefinition>();

            return _all
                .Where(r => r.Branch.Equals(branch.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}