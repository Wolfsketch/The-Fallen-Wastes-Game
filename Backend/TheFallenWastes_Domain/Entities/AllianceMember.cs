using System;
using TheFallenWastes_Domain.Enums;

namespace TheFallenWastes_Domain.Entities
{
    public class AllianceMember
    {
        public Guid Id { get; private set; }
        public Guid AllianceId { get; private set; }
        public Guid PlayerId { get; private set; }
        public AllianceMemberRank Rank { get; private set; }
        public DateTime JoinedAtUtc { get; private set; }

        private AllianceMember() { }

        public AllianceMember(Guid allianceId, Guid playerId, AllianceMemberRank rank = AllianceMemberRank.Member)
        {
            if (allianceId == Guid.Empty)
                throw new ArgumentException("AllianceId cannot be empty.", nameof(allianceId));
            if (playerId == Guid.Empty)
                throw new ArgumentException("PlayerId cannot be empty.", nameof(playerId));

            Id = Guid.NewGuid();
            AllianceId = allianceId;
            PlayerId = playerId;
            Rank = rank;
            JoinedAtUtc = DateTime.UtcNow;
        }

        public bool CanInvite { get; private set; }
        public bool CanManageRecruitment { get; private set; }
        public bool IsForumModerator { get; private set; }
        public bool CanBroadcast { get; private set; }
        public bool CanManageReservations { get; private set; }

        public void SetRank(AllianceMemberRank rank)
        {
            Rank = rank;
        }

        public void SetPermissions(bool canInvite, bool canManageRecruitment, bool isForumModerator, bool canBroadcast, bool canManageReservations)
        {
            CanInvite = canInvite;
            CanManageRecruitment = canManageRecruitment;
            IsForumModerator = isForumModerator;
            CanBroadcast = canBroadcast;
            CanManageReservations = canManageReservations;
        }
    }
}
