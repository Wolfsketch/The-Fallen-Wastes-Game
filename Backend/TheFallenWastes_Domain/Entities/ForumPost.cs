using System;

namespace TheFallenWastes_Domain.Entities
{
    public class ForumPost
    {
        public Guid Id { get; private set; }
        public Guid TopicId { get; private set; }
        public Guid AuthorPlayerId { get; private set; }
        public string AuthorUsername { get; private set; }
        public string Content { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }

        private ForumPost()
        {
            AuthorUsername = string.Empty;
            Content = string.Empty;
        }

        public ForumPost(Guid topicId, Guid authorPlayerId, string authorUsername, string content)
        {
            if (topicId == Guid.Empty) throw new ArgumentException("TopicId cannot be empty.", nameof(topicId));
            if (authorPlayerId == Guid.Empty) throw new ArgumentException("AuthorPlayerId cannot be empty.", nameof(authorPlayerId));
            if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("Content cannot be empty.", nameof(content));

            Id = Guid.NewGuid();
            TopicId = topicId;
            AuthorPlayerId = authorPlayerId;
            AuthorUsername = authorUsername?.Trim() ?? string.Empty;
            Content = content.Trim();
            CreatedAtUtc = DateTime.UtcNow;
        }
    }
}
