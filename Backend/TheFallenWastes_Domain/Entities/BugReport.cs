using System;

namespace TheFallenWastes_Domain.Entities
{
    public class BugReport
    {
        public Guid Id { get; private set; }
        public Guid PlayerId { get; private set; }
        public string Title { get; private set; }
        public string Category { get; private set; }
        public string Area { get; private set; }
        public string Severity { get; private set; }   // Low | Medium | High | Critical
        public string Description { get; private set; }
        public string StepsToReproduce { get; private set; }
        public string SettlementName { get; private set; }
        public string Browser { get; private set; }
        public string Status { get; private set; }     // Open | InProgress | Resolved | Closed
        public DateTime CreatedAtUtc { get; private set; }

        private BugReport()
        {
            Title = string.Empty;
            Category = string.Empty;
            Area = string.Empty;
            Severity = "Low";
            Description = string.Empty;
            StepsToReproduce = string.Empty;
            SettlementName = string.Empty;
            Browser = string.Empty;
            Status = "Open";
        }

        public BugReport(
            Guid playerId,
            string title,
            string category,
            string area,
            string severity,
            string description,
            string stepsToReproduce,
            string settlementName,
            string browser)
        {
            if (playerId == Guid.Empty) throw new ArgumentException("PlayerId cannot be empty.", nameof(playerId));
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title cannot be empty.", nameof(title));
            if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description cannot be empty.", nameof(description));

            Id = Guid.NewGuid();
            PlayerId = playerId;
            Title = title.Trim();
            Category = category?.Trim() ?? string.Empty;
            Area = area?.Trim() ?? string.Empty;
            Severity = severity?.Trim() ?? "Low";
            Description = description.Trim();
            StepsToReproduce = stepsToReproduce?.Trim() ?? string.Empty;
            SettlementName = settlementName?.Trim() ?? string.Empty;
            Browser = browser?.Trim() ?? string.Empty;
            Status = "Open";
            CreatedAtUtc = DateTime.UtcNow;
        }

        public void SetStatus(string status) => Status = status;
    }
}
