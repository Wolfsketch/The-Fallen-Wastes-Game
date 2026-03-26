using System;
using TheFallenWastes_Domain.Enums;

namespace TheFallenWastes_Domain.Entities
{
    public class Research
    {
        public Guid Id { get; private set; }

        /// <summary>
        /// Settlement-bound research.
        /// Research progress and unlocks belong to one settlement.
        /// </summary>
        public Guid SettlementId { get; private set; }

        /// <summary>
        /// Canonical research key, for example:
        /// "improved_ballistics", "convoy_protocols", "field_medicine"
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Display name shown in UI.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Optional description for UI and tooltips.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Branch/category for the research screen.
        /// Example values: Economy, Military, Defense, Logistics, Expansion, Salvage.
        /// </summary>
        public string Branch { get; private set; }

        /// <summary>
        /// Required Tech Lab level to start this research.
        /// </summary>
        public int RequiredTechLabLevel { get; private set; }

        /// <summary>
        /// Optional RareTech cost.
        /// </summary>
        public int RareTechCost { get; private set; }

        /// <summary>
        /// Optional Research Point cost or other future currency.
        /// </summary>
        public int ResearchPointCost { get; private set; }

        /// <summary>
        /// Base duration in seconds before Tech Lab speed modifiers.
        /// </summary>
        public int BaseDurationSeconds { get; private set; }

        /// <summary>
        /// Research state flags.
        /// </summary>
        public bool IsUnlocked { get; private set; }
        public bool IsResearching { get; private set; }

        /// <summary>
        /// Progress timing.
        /// </summary>
        public DateTime? StartedAtUtc { get; private set; }
        public DateTime? EndsAtUtc { get; private set; }

        private Research()
        {
            Key = string.Empty;
            Name = string.Empty;
            Description = string.Empty;
            Branch = string.Empty;
        }

        public Research(
            Guid settlementId,
            string key,
            string name,
            string description,
            string branch,
            int requiredTechLabLevel,
            int rareTechCost,
            int researchPointCost,
            int baseDurationSeconds)
        {
            if (settlementId == Guid.Empty)
                throw new ArgumentException("SettlementId cannot be empty.", nameof(settlementId));

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
                throw new ArgumentException("Research point cost cannot be negative.", nameof(researchPointCost));

            if (baseDurationSeconds <= 0)
                throw new ArgumentException("Base duration must be greater than zero.", nameof(baseDurationSeconds));

            Id = Guid.NewGuid();
            SettlementId = settlementId;
            Key = key.Trim().ToLowerInvariant();
            Name = name.Trim();
            Description = description?.Trim() ?? string.Empty;
            Branch = branch.Trim();

            RequiredTechLabLevel = requiredTechLabLevel;
            RareTechCost = rareTechCost;
            ResearchPointCost = researchPointCost;
            BaseDurationSeconds = baseDurationSeconds;

            IsUnlocked = false;
            IsResearching = false;
            StartedAtUtc = null;
            EndsAtUtc = null;
        }

        public void Start(DateTime startedAtUtc, int effectiveDurationSeconds)
        {
            if (IsUnlocked)
                throw new InvalidOperationException($"Research '{Name}' is already unlocked.");

            if (IsResearching)
                throw new InvalidOperationException($"Research '{Name}' is already in progress.");

            if (effectiveDurationSeconds <= 0)
                throw new ArgumentException("Effective duration must be greater than zero.", nameof(effectiveDurationSeconds));

            StartedAtUtc = startedAtUtc;
            EndsAtUtc = startedAtUtc.AddSeconds(effectiveDurationSeconds);
            IsResearching = true;
        }

        public void Cancel()
        {
            if (!IsResearching)
                throw new InvalidOperationException($"Research '{Name}' is not in progress.");

            IsResearching = false;
            StartedAtUtc = null;
            EndsAtUtc = null;
        }

        public bool CanComplete(DateTime utcNow)
        {
            return IsResearching && EndsAtUtc.HasValue && utcNow >= EndsAtUtc.Value;
        }

        public void Complete(DateTime utcNow)
        {
            if (!CanComplete(utcNow))
                throw new InvalidOperationException($"Research '{Name}' cannot be completed yet.");

            IsResearching = false;
            IsUnlocked = true;
            StartedAtUtc = null;
            EndsAtUtc = null;
        }

        public int GetRemainingSeconds(DateTime utcNow)
        {
            if (!IsResearching || !EndsAtUtc.HasValue)
                return 0;

            var remaining = (int)Math.Ceiling((EndsAtUtc.Value - utcNow).TotalSeconds);
            return Math.Max(0, remaining);
        }

        public bool MeetsTechLabRequirement(int techLabLevel)
        {
            return techLabLevel >= RequiredTechLabLevel;
        }
    }
}