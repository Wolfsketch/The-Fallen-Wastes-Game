using System;
using TheFallenWastes_Domain.Enums;

namespace TheFallenWastes_Domain.Entities
{
    public class BuildingUpgradeQueueItem
    {
        public Guid Id { get; private set; }

        public Guid SettlementId { get; private set; }
        public Settlement Settlement { get; private set; } = null!;

        public BuildingType BuildingType { get; private set; }
        public int TargetLevel { get; private set; }

        // Snapshot of cost at time of queuing
        public int CostWater { get; private set; }
        public int CostFood { get; private set; }
        public int CostScrap { get; private set; }
        public int CostFuel { get; private set; }
        public int CostEnergy { get; private set; }
        public int CostRareTech { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }

        // When started this will be set; until then the item is waiting
        public bool IsStarted { get; private set; }
        public DateTime? StartedAtUtc { get; private set; }
        public DateTime? EndsAtUtc { get; private set; }
        public Guid? ActiveBuildingId { get; private set; }

        private BuildingUpgradeQueueItem() { }

        public BuildingUpgradeQueueItem(
            Guid settlementId,
            BuildingType buildingType,
            int targetLevel,
            int costWater,
            int costFood,
            int costScrap,
            int costFuel,
            int costEnergy,
            int costRareTech)
        {
            if (settlementId == Guid.Empty)
                throw new ArgumentException("SettlementId cannot be empty.", nameof(settlementId));

            Id = Guid.NewGuid();
            SettlementId = settlementId;
            BuildingType = buildingType;
            TargetLevel = targetLevel;

            CostWater = costWater;
            CostFood = costFood;
            CostScrap = costScrap;
            CostFuel = costFuel;
            CostEnergy = costEnergy;
            CostRareTech = costRareTech;

            CreatedAtUtc = DateTime.UtcNow;

            IsStarted = false;
            StartedAtUtc = null;
            EndsAtUtc = null;
        }

        public void MarkStarted(int durationSeconds)
        {
            if (IsStarted) return;
            IsStarted = true;
            StartedAtUtc = DateTime.UtcNow;
            EndsAtUtc = StartedAtUtc.Value.AddSeconds(durationSeconds);
            ActiveBuildingId = null;
        }

        public void MarkStarted(int durationSeconds, Guid buildingId)
        {
            if (IsStarted) return;
            IsStarted = true;
            StartedAtUtc = DateTime.UtcNow;
            EndsAtUtc = StartedAtUtc.Value.AddSeconds(durationSeconds);
            ActiveBuildingId = buildingId;
        }
    }
}
