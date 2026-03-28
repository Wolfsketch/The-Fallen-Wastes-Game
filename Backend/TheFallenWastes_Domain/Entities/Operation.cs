using System;

namespace TheFallenWastes_Domain.Entities
{
    public class Operation
    {
        public Guid Id { get; private set; }
        public Guid AttackerSettlementId { get; private set; }
        public Guid? TargetSettlementId { get; private set; }   // null = POI aanval
        public string? TargetPoiId { get; private set; }         // null = settlement aanval
        public string? TargetPoiLabel { get; private set; }        // human-readable POI name

        public string OperationType { get; private set; }
        // values: "scout_poi", "scout_settlement", "raid_poi", "attack_settlement", "reinforce_poi"

        public string Phase { get; private set; }
        // values: "outbound", "arrived", "returning", "completed"

        // Snapshot of sent units: JSON dict unitName -> quantity
        public string SentUnitsJson { get; private set; }

        // For scout: how much RareTech was sent
        public int? ScoutRareTech { get; private set; }

        // For raid POI: which mode
        public string? RaidMode { get; private set; }

        public DateTime StartedAtUtc { get; private set; }
        public DateTime ArrivesAtUtc { get; private set; }
        public DateTime? ReturnsAtUtc { get; private set; }
        public DateTime? CompletedAtUtc { get; private set; }

        // Result stored after arrival resolution
        public string? ResultJson { get; private set; }

        private Operation() { }

        public Operation(
            Guid attackerSettlementId,
            Guid? targetSettlementId,
            string? targetPoiId,
            string? targetPoiLabel,
            string operationType,
            string sentUnitsJson,
            int? scoutRareTech,
            string? raidMode,
            int travelSeconds)
        {
            Id = Guid.NewGuid();
            AttackerSettlementId = attackerSettlementId;
            TargetSettlementId = targetSettlementId;
            TargetPoiId = targetPoiId;
            TargetPoiLabel = targetPoiLabel;
            OperationType = operationType;
            Phase = "outbound";
            SentUnitsJson = sentUnitsJson;
            ScoutRareTech = scoutRareTech;
            RaidMode = raidMode;
            StartedAtUtc = DateTime.UtcNow;
            ArrivesAtUtc = DateTime.UtcNow.AddSeconds(travelSeconds);
            ReturnsAtUtc = null;
            CompletedAtUtc = null;
            ResultJson = null;
        }

        public void MarkArrived() { Phase = "arrived"; }
        public void MarkReturning(int returnSeconds)
        {
            Phase = "returning";
            ReturnsAtUtc = DateTime.UtcNow.AddSeconds(returnSeconds);
        }
        public void MarkCompleted(string resultJson)
        {
            Phase = "completed";
            CompletedAtUtc = DateTime.UtcNow;
            ResultJson = resultJson;
        }
        public void SetResult(string resultJson) { ResultJson = resultJson; }
    }
}
