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

        /// <summary>How many units from this batch have already been delivered to the settlement inventory.</summary>
        public int DeliveredQuantity { get; private set; }

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
            if (IsCompleted || DeliveredQuantity >= Quantity)
                return 0;

            // Time until the NEXT individual unit is ready
            double totalSeconds = (EndsAtUtc - StartedAtUtc).TotalSeconds;
            int perUnit = Math.Max(1, (int)Math.Round(totalSeconds / Quantity));
            int nextDeliveryAt = perUnit * (DeliveredQuantity + 1);
            var remaining = StartedAtUtc.AddSeconds(nextDeliveryAt) - DateTime.UtcNow;
            return remaining.TotalSeconds > 0 ? (int)Math.Ceiling(remaining.TotalSeconds) : 0;
        }

        /// <summary>Seconds until the entire batch is complete.</summary>
        public int GetTotalRemainingSeconds()
        {
            if (IsCompleted)
                return 0;
            var remaining = (EndsAtUtc - DateTime.UtcNow).TotalSeconds;
            return remaining > 0 ? (int)Math.Ceiling(remaining) : 0;
        }

        public bool IsReadyToComplete()
        {
            return !IsCompleted && DateTime.UtcNow >= EndsAtUtc;
        }

        public int GetTotalPopulationCost()
        {
            return Quantity * PopulationCostPerUnit;
        }

        /// <summary>Deliver a number of units from this batch (partial completion).</summary>
        public void DeliverUnits(int count)
        {
            if (count <= 0) return;
            DeliveredQuantity = Math.Min(Quantity, DeliveredQuantity + count);
        }

        public void MarkCompleted()
        {
            if (IsCompleted)
                return;

            DeliveredQuantity = Quantity;
            IsCompleted = true;
            CompletedAtUtc = DateTime.UtcNow;
        }
    }
}