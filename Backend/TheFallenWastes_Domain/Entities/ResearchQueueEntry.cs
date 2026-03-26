using System;

namespace TheFallenWastes_Domain.Entities
{
    public class ResearchQueueEntry
    {
        public Guid Id { get; private set; }

        public Guid SettlementResearchStateId { get; private set; }

        /// <summary>
        /// Canonical research key, linked to ResearchDefinition.Key
        /// </summary>
        public string ResearchKey { get; private set; }

        /// <summary>
        /// Queue slot or ordering index.
        /// Slot 1 = first/primary active slot.
        /// </summary>
        public int SlotIndex { get; private set; }

        /// <summary>
        /// Ordering inside the queue if later you support waiting entries behind an active one.
        /// </summary>
        public int QueueOrder { get; private set; }

        public bool IsActive { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool IsCancelled { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }
        public DateTime? StartedAtUtc { get; private set; }
        public DateTime? EndsAtUtc { get; private set; }
        public DateTime? CompletedAtUtc { get; private set; }
        public DateTime? CancelledAtUtc { get; private set; }

        private ResearchQueueEntry()
        {
            ResearchKey = string.Empty;
        }

        public ResearchQueueEntry(
            Guid settlementResearchStateId,
            string researchKey,
            int slotIndex,
            int queueOrder)
        {
            if (settlementResearchStateId == Guid.Empty)
                throw new ArgumentException("SettlementResearchStateId cannot be empty.", nameof(settlementResearchStateId));

            if (string.IsNullOrWhiteSpace(researchKey))
                throw new ArgumentException("ResearchKey cannot be empty.", nameof(researchKey));

            if (slotIndex <= 0)
                throw new ArgumentException("SlotIndex must be greater than zero.", nameof(slotIndex));

            if (queueOrder < 0)
                throw new ArgumentException("QueueOrder cannot be negative.", nameof(queueOrder));

            Id = Guid.NewGuid();
            SettlementResearchStateId = settlementResearchStateId;
            ResearchKey = researchKey.Trim().ToLowerInvariant();
            SlotIndex = slotIndex;
            QueueOrder = queueOrder;

            IsActive = false;
            IsCompleted = false;
            IsCancelled = false;

            CreatedAtUtc = DateTime.UtcNow;
        }

        public void Start(DateTime startedAtUtc, int durationSeconds)
        {
            if (IsCompleted)
                throw new InvalidOperationException("Queue entry is already completed.");

            if (IsCancelled)
                throw new InvalidOperationException("Queue entry is cancelled.");

            if (IsActive)
                throw new InvalidOperationException("Queue entry is already active.");

            if (durationSeconds <= 0)
                throw new ArgumentException("Duration must be greater than zero.", nameof(durationSeconds));

            StartedAtUtc = startedAtUtc;
            EndsAtUtc = startedAtUtc.AddSeconds(durationSeconds);
            IsActive = true;
        }

        public bool CanComplete(DateTime utcNow)
        {
            return IsActive && !IsCompleted && !IsCancelled && EndsAtUtc.HasValue && utcNow >= EndsAtUtc.Value;
        }

        public void Complete(DateTime utcNow)
        {
            if (!CanComplete(utcNow))
                throw new InvalidOperationException("Queue entry cannot be completed yet.");

            IsActive = false;
            IsCompleted = true;
            CompletedAtUtc = utcNow;
        }

        public void Cancel(DateTime utcNow)
        {
            if (IsCompleted)
                throw new InvalidOperationException("Completed queue entries cannot be cancelled.");

            if (IsCancelled)
                throw new InvalidOperationException("Queue entry is already cancelled.");

            IsActive = false;
            IsCancelled = true;
            CancelledAtUtc = utcNow;
        }

        public int GetRemainingSeconds(DateTime utcNow)
        {
            if (!IsActive || !EndsAtUtc.HasValue)
                return 0;

            var remaining = (int)Math.Ceiling((EndsAtUtc.Value - utcNow).TotalSeconds);
            return Math.Max(0, remaining);
        }

        public void SetQueueOrder(int queueOrder)
        {
            if (queueOrder < 0)
                throw new ArgumentException("QueueOrder cannot be negative.", nameof(queueOrder));

            QueueOrder = queueOrder;
        }

        public void SetSlotIndex(int slotIndex)
        {
            if (slotIndex <= 0)
                throw new ArgumentException("SlotIndex must be greater than zero.", nameof(slotIndex));

            SlotIndex = slotIndex;
        }
    }
}