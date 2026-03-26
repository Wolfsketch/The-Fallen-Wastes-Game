using System;
using System.Collections.Generic;
using System.Linq;

namespace TheFallenWastes_Domain.Entities
{
    public class SettlementResearchState
    {
        public Guid Id { get; private set; }
        public Guid SettlementId { get; private set; }

        /// <summary>
        /// Research points/capacity available for this settlement.
        /// Later this can be derived from Tech Lab / Council Hall / other systems if needed.
        /// </summary>
        public int ResearchPoints { get; private set; }

        /// <summary>
        /// Maximum number of researches that can run at the same time.
        /// Start with 1, can later scale with Tech Lab milestones.
        /// </summary>
        public int MaxConcurrentResearches { get; private set; }

        /// <summary>
        /// All research entries belonging to this settlement.
        /// One entry per research key.
        /// </summary>
        public ICollection<Research> Researches { get; private set; }

        private SettlementResearchState()
        {
            Researches = new List<Research>();
        }

        public SettlementResearchState(Guid settlementId, int researchPoints = 1, int maxConcurrentResearches = 1)
        {
            if (settlementId == Guid.Empty)
                throw new ArgumentException("SettlementId cannot be empty.", nameof(settlementId));

            if (researchPoints < 0)
                throw new ArgumentException("ResearchPoints cannot be negative.", nameof(researchPoints));

            if (maxConcurrentResearches <= 0)
                throw new ArgumentException("MaxConcurrentResearches must be at least 1.", nameof(maxConcurrentResearches));

            Id = Guid.NewGuid();
            SettlementId = settlementId;
            ResearchPoints = researchPoints;
            MaxConcurrentResearches = maxConcurrentResearches;
            Researches = new List<Research>();
        }

        public IReadOnlyCollection<Research> GetUnlockedResearches()
        {
            return Researches
                .Where(r => r.IsUnlocked)
                .ToList();
        }

        public IReadOnlyCollection<Research> GetActiveResearches()
        {
            return Researches
                .Where(r => r.IsResearching)
                .ToList();
        }

        public bool HasUnlocked(string researchKey)
        {
            if (string.IsNullOrWhiteSpace(researchKey))
                return false;

            return Researches.Any(r =>
                r.Key.Equals(researchKey.Trim(), StringComparison.OrdinalIgnoreCase) &&
                r.IsUnlocked);
        }

        public bool IsResearchInProgress(string researchKey)
        {
            if (string.IsNullOrWhiteSpace(researchKey))
                return false;

            return Researches.Any(r =>
                r.Key.Equals(researchKey.Trim(), StringComparison.OrdinalIgnoreCase) &&
                r.IsResearching);
        }

        public Research? GetResearch(string researchKey)
        {
            if (string.IsNullOrWhiteSpace(researchKey))
                return null;

            return Researches.FirstOrDefault(r =>
                r.Key.Equals(researchKey.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public void AddResearch(Research research)
        {
            if (research == null)
                throw new ArgumentNullException(nameof(research));

            if (research.SettlementId != SettlementId)
                throw new InvalidOperationException("Research does not belong to this settlement.");

            if (Researches.Any(r => r.Key.Equals(research.Key, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"Research '{research.Key}' already exists for this settlement.");

            Researches.Add(research);
        }

        public int GetAvailableResearchSlots()
        {
            int active = Researches.Count(r => r.IsResearching);
            return Math.Max(0, MaxConcurrentResearches - active);
        }

        public bool CanStartAnotherResearch()
        {
            return GetAvailableResearchSlots() > 0;
        }

        public void SetResearchPoints(int value)
        {
            if (value < 0)
                throw new ArgumentException("ResearchPoints cannot be negative.", nameof(value));

            ResearchPoints = value;
        }

        public void SetMaxConcurrentResearches(int value)
        {
            if (value <= 0)
                throw new ArgumentException("MaxConcurrentResearches must be at least 1.", nameof(value));

            MaxConcurrentResearches = value;
        }

        public void CompleteReadyResearches(DateTime utcNow)
        {
            foreach (var research in Researches.Where(r => r.CanComplete(utcNow)).ToList())
            {
                research.Complete(utcNow);
            }
        }
    }
}