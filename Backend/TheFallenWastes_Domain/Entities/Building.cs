using System;
using TheFallenWastes_Domain.Enums;

namespace TheFallenWastes_Domain.Entities
{
    public class Building
    {
        public Guid Id { get; private set; }
        public Guid SettlementId { get; private set; }
        public BuildingType Type { get; private set; }
        public int Level { get; private set; }

        public bool IsConstructing { get; private set; }
        public DateTime? ConstructionStartUtc { get; private set; }
        public DateTime? ConstructionEndUtc { get; private set; }
        public int TargetLevel { get; private set; }

        private Building() { }

        public Building(Guid settlementId, BuildingType type)
        {
            if (settlementId == Guid.Empty)
                throw new ArgumentException("SettlementId cannot be empty.", nameof(settlementId));

            Id = Guid.NewGuid();
            SettlementId = settlementId;
            Type = type;
            Level = 0;
            IsConstructing = false;
            TargetLevel = 0;
        }

        /// <summary>
        /// Factory method: create a building already at a specific level (for starter buildings).
        /// </summary>
        public static Building CreateAtLevel(Guid settlementId, BuildingType type, int level)
        {
            var building = new Building(settlementId, type);
            building.Level = level;
            return building;
        }

        public void StartUpgrade(int buildTimeSeconds)
        {
            if (IsConstructing)
                throw new InvalidOperationException("Building is already under construction.");
            // default behaviour: upgrade to next level
            StartUpgradeToLevel(Level + 1, buildTimeSeconds);
        }

        public void StartUpgradeToLevel(int targetLevel, int buildTimeSeconds)
        {
            if (IsConstructing)
                throw new InvalidOperationException("Building is already under construction.");

            if (targetLevel <= Level)
                throw new InvalidOperationException("Target level must be greater than current level.");

            TargetLevel = targetLevel;
            IsConstructing = true;
            ConstructionStartUtc = DateTime.UtcNow;
            ConstructionEndUtc = DateTime.UtcNow.AddSeconds(buildTimeSeconds);
        }

        public bool TryCompleteConstruction()
        {
            if (!IsConstructing || ConstructionEndUtc == null)
                return false;

            if (DateTime.UtcNow < ConstructionEndUtc)
                return false;

            Level = TargetLevel;
            IsConstructing = false;
            ConstructionStartUtc = null;
            ConstructionEndUtc = null;
            TargetLevel = 0;
            return true;
        }

        public void CancelConstruction()
        {
            if (!IsConstructing)
                throw new InvalidOperationException("No construction to cancel.");

            IsConstructing = false;
            ConstructionStartUtc = null;
            ConstructionEndUtc = null;
            TargetLevel = 0;
        }

        public int GetRemainingSeconds()
        {
            if (!IsConstructing || ConstructionEndUtc == null)
                return 0;

            var remaining = (ConstructionEndUtc.Value - DateTime.UtcNow).TotalSeconds;
            return remaining > 0 ? (int)Math.Ceiling(remaining) : 0;
        }
    }
}