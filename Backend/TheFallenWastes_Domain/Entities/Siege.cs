using System;

namespace TheFallenWastes_Domain.Entities
{
    public class Siege
    {
        /// <summary>Duration of a siege in seconds. Default: 12 hours (43200). Configurable.</summary>
        public const int SiegeDurationSeconds = 43200;

        public Guid Id { get; private set; }

        /// <summary>The settlement that is under siege.</summary>
        public Guid SettlementId { get; private set; }

        public Guid AttackerPlayerId { get; private set; }
        public Guid DefenderPlayerId { get; private set; }

        /// <summary>The attack_settlement operation that started this siege.</summary>
        public Guid ConvoyOperationId { get; private set; }

        public DateTime StartedAtUtc { get; private set; }
        public DateTime EndsAtUtc { get; private set; }

        /// <summary>Status values: "Active" | "ConquestSuccessful" | "ConquestFailed"</summary>
        public string Status { get; private set; } = "Active";

        public DateTime? CompletedAtUtc { get; private set; }

        /// <summary>JSON dict (unitName -> quantity) of the current garrison defending the siege.</summary>
        public string GarrisonUnitsJson { get; private set; } = "{}";

        private Siege() { }

        public Siege(
            Guid settlementId,
            Guid attackerPlayerId,
            Guid defenderPlayerId,
            Guid convoyOperationId,
            string garrisonUnitsJson)
        {
            Id = Guid.NewGuid();
            SettlementId = settlementId;
            AttackerPlayerId = attackerPlayerId;
            DefenderPlayerId = defenderPlayerId;
            ConvoyOperationId = convoyOperationId;
            StartedAtUtc = DateTime.UtcNow;
            EndsAtUtc = DateTime.UtcNow.AddSeconds(SiegeDurationSeconds);
            Status = "Active";
            GarrisonUnitsJson = garrisonUnitsJson;
        }

        public bool IsActive => Status == "Active";
        public bool IsExpired => DateTime.UtcNow >= EndsAtUtc && Status == "Active";

        public void UpdateGarrison(string garrisonUnitsJson)
        {
            GarrisonUnitsJson = garrisonUnitsJson;
        }

        public void ResolveConquest()
        {
            Status = "ConquestSuccessful";
            CompletedAtUtc = DateTime.UtcNow;
        }

        public void ResolveFailed()
        {
            Status = "ConquestFailed";
            CompletedAtUtc = DateTime.UtcNow;
        }
    }
}
