using System;

namespace TheFallenWastes_Domain.Entities
{
    public class AllianceForumPost
    {
        public Guid Id { get; private set; }
        public Guid TopicId { get; private set; }
        public Guid AuthorPlayerId { get; private set; }
        public string Content { get; private set; }
        public DateTime CreatedAtUtc { get; private set; }
        public DateTime? EditedAtUtc { get; private set; }

        private AllianceForumPost()
        {
            Content = string.Empty;
        }

        public AllianceForumPost(Guid topicId, Guid authorPlayerId, string content)
        {
            if (topicId == Guid.Empty)
                throw new ArgumentException("TopicId cannot be empty.", nameof(topicId));
            if (authorPlayerId == Guid.Empty)
                throw new ArgumentException("AuthorPlayerId cannot be empty.", nameof(authorPlayerId));
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Content cannot be empty.", nameof(content));

            Id = Guid.NewGuid();
            TopicId = topicId;
            AuthorPlayerId = authorPlayerId;
            Content = content.Trim();
            CreatedAtUtc = DateTime.UtcNow;
        }

        public void Edit(string newContent)
        {
            if (string.IsNullOrWhiteSpace(newContent))
                throw new ArgumentException("Content cannot be empty.", nameof(newContent));
            Content = newContent.Trim();
            EditedAtUtc = DateTime.UtcNow;
        }
    }
}
