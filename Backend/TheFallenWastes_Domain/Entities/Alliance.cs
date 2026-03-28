using System;
using System.Collections.Generic;
using TheFallenWastes_Domain.Enums;

namespace TheFallenWastes_Domain.Entities
{
    public class Alliance
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public string Tag { get; private set; }
        public string Description { get; private set; }
        public AllianceStatus Status { get; private set; }
        public int MinPoints { get; private set; }
        public Guid FounderPlayerId { get; private set; }
        public DateTime FoundedAtUtc { get; private set; }

        public ICollection<AllianceMember> Members { get; private set; }
        public ICollection<AllianceApplication> Applications { get; private set; }
        public ICollection<AllianceForumTopic> ForumTopics { get; private set; }

        private Alliance()
        {
            Name = string.Empty;
            Tag = string.Empty;
            Description = string.Empty;
            Members = new List<AllianceMember>();
            Applications = new List<AllianceApplication>();
            ForumTopics = new List<AllianceForumTopic>();
        }

        public Alliance(string name, string tag, Guid founderPlayerId)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Alliance name cannot be empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentException("Alliance tag cannot be empty.", nameof(tag));
            if (tag.Length > 6)
                throw new ArgumentException("Alliance tag cannot exceed 6 characters.", nameof(tag));
            if (founderPlayerId == Guid.Empty)
                throw new ArgumentException("FounderPlayerId cannot be empty.", nameof(founderPlayerId));

            Id = Guid.NewGuid();
            Name = name.Trim();
            Tag = tag.Trim().ToUpperInvariant();
            Description = string.Empty;
            Status = AllianceStatus.ApplicationRequired;
            MinPoints = 0;
            FounderPlayerId = founderPlayerId;
            FoundedAtUtc = DateTime.UtcNow;

            Members = new List<AllianceMember>();
            Applications = new List<AllianceApplication>();
            ForumTopics = new List<AllianceForumTopic>();
        }

        public void UpdateSettings(string name, string tag, string description, AllianceStatus status, int minPoints)
        {
            if (!string.IsNullOrWhiteSpace(name)) Name = name.Trim();
            if (!string.IsNullOrWhiteSpace(tag))
            {
                if (tag.Length > 6)
                    throw new ArgumentException("Alliance tag cannot exceed 6 characters.", nameof(tag));
                Tag = tag.Trim().ToUpperInvariant();
            }
            Description = description ?? string.Empty;
            Status = status;
            MinPoints = Math.Max(0, minPoints);
        }
    }
}
