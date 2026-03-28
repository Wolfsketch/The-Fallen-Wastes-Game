using System;

namespace TheFallenWastes_Domain.Entities
{
    public class AllianceApplication
    {
        public Guid Id { get; private set; }
        public Guid AllianceId { get; private set; }
        public Guid PlayerId { get; private set; }
        public string Message { get; private set; }
        public bool IsInvitation { get; private set; }   // true = invite from alliance, false = application from player
        public string Status { get; private set; }       // "Pending" | "Accepted" | "Rejected"
        public DateTime CreatedAtUtc { get; private set; }

        private AllianceApplication()
        {
            Message = string.Empty;
            Status = "Pending";
        }

        public AllianceApplication(Guid allianceId, Guid playerId, string message, bool isInvitation = false)
        {
            if (allianceId == Guid.Empty)
                throw new ArgumentException("AllianceId cannot be empty.", nameof(allianceId));
            if (playerId == Guid.Empty)
                throw new ArgumentException("PlayerId cannot be empty.", nameof(playerId));

            Id = Guid.NewGuid();
            AllianceId = allianceId;
            PlayerId = playerId;
            Message = message ?? string.Empty;
            IsInvitation = isInvitation;
            Status = "Pending";
            CreatedAtUtc = DateTime.UtcNow;
        }

        public void Accept() => Status = "Accepted";
        public void Reject() => Status = "Rejected";
    }
}
