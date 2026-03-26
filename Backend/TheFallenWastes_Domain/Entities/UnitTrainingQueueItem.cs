using System;

namespace TheFallenWastes_Domain.Entities
{
    public class UnitTrainingQueueItem
    {
        public Guid Id { get; private set; }

        public Guid SettlementId { get; private set; }
        public Settlement Settlement { get; private set; } = null!;

        public string UnitName { get; private set; } = string.Empty;
        public int Quantity { get; private set; }
        public int PopulationCostPerUnit { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }
        public DateTime StartedAtUtc { get; private set; }
        public DateTime EndsAtUtc { get; private set; }

        public bool IsCompleted { get; private set; }
        public DateTime? CompletedAtUtc { get; private set; }

        private UnitTrainingQueueItem() { }

        public UnitTrainingQueueItem(
            Guid settlementId,
            string unitName,
            int quantity,
            int populationCostPerUnit,
            int durationSeconds)
        {
            if (settlementId == Guid.Empty)
                throw new ArgumentException("SettlementId cannot be empty.", nameof(settlementId));

            if (string.IsNullOrWhiteSpace(unitName))
                throw new ArgumentException("Unit name cannot be empty.", nameof(unitName));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

            if (populationCostPerUnit < 0)
                throw new ArgumentException("Population cost cannot be negative.", nameof(populationCostPerUnit));

            if (durationSeconds <= 0)
                throw new ArgumentException("Duration must be greater than zero.", nameof(durationSeconds));

            Id = Guid.NewGuid();
            SettlementId = settlementId;
            UnitName = unitName;
            Quantity = quantity;
            PopulationCostPerUnit = populationCostPerUnit;

            CreatedAtUtc = DateTime.UtcNow;
            StartedAtUtc = CreatedAtUtc;
            EndsAtUtc = StartedAtUtc.AddSeconds(durationSeconds);

            IsCompleted = false;
            CompletedAtUtc = null;
        }

        public int GetRemainingSeconds()
        {
            if (IsCompleted)
                return 0;

            var remaining = (int)Math.Ceiling((EndsAtUtc - DateTime.UtcNow).TotalSeconds);
            return Math.Max(0, remaining);
        }

        public bool IsReadyToComplete()
        {
            return !IsCompleted && DateTime.UtcNow >= EndsAtUtc;
        }

        public int GetTotalPopulationCost()
        {
            return Quantity * PopulationCostPerUnit;
        }

        public void MarkCompleted()
        {
            if (IsCompleted)
                return;

            IsCompleted = true;
            CompletedAtUtc = DateTime.UtcNow;
        }
    }
}