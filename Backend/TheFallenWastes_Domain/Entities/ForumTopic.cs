using System;
using System.Collections.Generic;

namespace TheFallenWastes_Domain.Entities
{
    public class ForumTopic
    {
        public Guid Id { get; private set; }
        public string CategoryKey { get; private set; }   // e.g. "suggestions", "q-and-a"
        public string Title { get; private set; }
        public Guid AuthorPlayerId { get; private set; }
        public string AuthorUsername { get; private set; }
        public bool IsPinned { get; private set; }
        public bool IsOfficial { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }
        public DateTime LastPostAtUtc { get; private set; }
        public Guid? LastPostPlayerId { get; private set; }
        public string LastPostUsername { get; private set; }

        public ICollection<ForumPost> Posts { get; private set; }

        private ForumTopic()
        {
            CategoryKey = string.Empty;
            Title = string.Empty;
            AuthorUsername = string.Empty;
            LastPostUsername = string.Empty;
            Posts = new List<ForumPost>();
        }

        public ForumTopic(string categoryKey, string title, Guid authorPlayerId, string authorUsername, bool isOfficial = false)
        {
            if (string.IsNullOrWhiteSpace(categoryKey)) throw new ArgumentException("CategoryKey cannot be empty.", nameof(categoryKey));
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title cannot be empty.", nameof(title));
            if (authorPlayerId == Guid.Empty) throw new ArgumentException("AuthorPlayerId cannot be empty.", nameof(authorPlayerId));

            Id = Guid.NewGuid();
            CategoryKey = categoryKey.ToLowerInvariant().Trim();
            Title = title.Trim();
            AuthorPlayerId = authorPlayerId;
            AuthorUsername = authorUsername?.Trim() ?? string.Empty;
            IsOfficial = isOfficial;
            IsPinned = false;
            CreatedAtUtc = DateTime.UtcNow;
            LastPostAtUtc = DateTime.UtcNow;
            LastPostPlayerId = authorPlayerId;
            LastPostUsername = AuthorUsername;
            Posts = new List<ForumPost>();
        }

        public void SetPinned(bool pinned) => IsPinned = pinned;

        public void UpdateLastPost(Guid playerId, string username)
        {
            LastPostPlayerId = playerId;
            LastPostUsername = username?.Trim() ?? string.Empty;
            LastPostAtUtc = DateTime.UtcNow;
        }
    }
}
