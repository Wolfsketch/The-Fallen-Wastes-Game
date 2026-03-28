using System;
using System.Collections.Generic;

namespace TheFallenWastes_Domain.Entities
{
    public class AllianceForumTopic
    {
        public Guid Id { get; private set; }
        public Guid AllianceId { get; private set; }
        public string Title { get; private set; }
        public Guid AuthorPlayerId { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }
        public bool IsPinned { get; private set; }

        public ICollection<AllianceForumPost> Posts { get; private set; }

        private AllianceForumTopic()
        {
            Title = string.Empty;
            Posts = new List<AllianceForumPost>();
        }

        public AllianceForumTopic(Guid allianceId, string title, Guid authorPlayerId)
        {
            if (allianceId == Guid.Empty)
                throw new ArgumentException("AllianceId cannot be empty.", nameof(allianceId));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title cannot be empty.", nameof(title));
            if (authorPlayerId == Guid.Empty)
                throw new ArgumentException("AuthorPlayerId cannot be empty.", nameof(authorPlayerId));

            Id = Guid.NewGuid();
            AllianceId = allianceId;
            Title = title.Trim();
            AuthorPlayerId = authorPlayerId;
            CreatedAtUtc = DateTime.UtcNow;
            IsPinned = false;
            Posts = new List<AllianceForumPost>();
        }

        public void SetPinned(bool pinned) => IsPinned = pinned;
    }
}
