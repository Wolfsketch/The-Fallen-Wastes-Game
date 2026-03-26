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
                description: "Verhoogt de efficiëntie van Food-verbruik en interne distributie.",
                branch: "Economy",
                requiredTechLabLevel: 1,
                rareTechCost: 10,
                researchPointCost: 1,
                baseDurationSeconds: 1800
            ),
            new ResearchDefinition(
                key: "water_recycling",
                name: "Water Recycling",
                description: "Verhoogt de efficiëntie van Water-verwerking in de settlement.",
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
                description: "Verbetert de opbrengst van herwonnen Scrap-materialen.",
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
                description: "Verbetert Ballistic-wapenplatformen en munitie-output.",
                branch: "Military",
                requiredTechLabLevel: 2,
                rareTechCost: 16,
                researchPointCost: 1,
                baseDurationSeconds: 3000
            ),
            new ResearchDefinition(
                key: "impact_plating",
                name: "Impact Plating",
                description: "Verhoogt bescherming tegen zware Impact-aanvallen.",
                branch: "Military",
                requiredTechLabLevel: 3,
                rareTechCost: 22,
                researchPointCost: 2,
                baseDurationSeconds: 4200
            ),
            new ResearchDefinition(
                key: "energy_focusing",
                name: "Energy Focusing",
                description: "Verbetert stabiliteit en output van Energy-gebaseerde wapens.",
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
                description: "Versterkt de structurele efficiëntie van Perimeter Wall-systemen.",
                branch: "Defense",
                requiredTechLabLevel: 3,
                rareTechCost: 24,
                researchPointCost: 2,
                baseDurationSeconds: 4200
            ),
            new ResearchDefinition(
                key: "emergency_response_drills",
                name: "Emergency Response Drills",
                description: "Verbetert reactietijd van verdediging en interne paraatheid.",
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
                description: "Ontgrendelt en ondersteunt geavanceerde Convoy-operaties.",
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
                description: "Verbetert transportcoördinatie en operationele inzetbaarheid.",
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
                description: "Verhoogt de efficiëntie van de Tech Salvager bij gewone recoveries.",
                branch: "Salvage",
                requiredTechLabLevel: 2,
                rareTechCost: 15,
                researchPointCost: 1,
                baseDurationSeconds: 2400
            ),
            new ResearchDefinition(
                key: "datacore_reconstruction",
                name: "Datacore Reconstruction",
                description: "Maakt complexere event- en POI-artefacten bruikbaar voor research.",
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
                description: "Verbetert bestuurlijke coördinatie tussen meerdere settlements.",
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