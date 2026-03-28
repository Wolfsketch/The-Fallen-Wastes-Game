using System;

namespace TheFallenWastes_Domain.Entities
{
    public class PoiState
    {
        public Guid Id { get; private set; }
        public string PoiId { get; private set; }
        public bool IsCleared { get; private set; }
        public DateTime? ClearedAtUtc { get; private set; }
        public DateTime NextRespawnUtc { get; private set; }

        private PoiState() { }

        public PoiState(string poiId)
        {
            Id = Guid.NewGuid();
            PoiId = poiId;
            IsCleared = false;
            NextRespawnUtc = DateTime.UtcNow.AddHours(12);
        }

        public void MarkCleared()
        {
            IsCleared = true;
            ClearedAtUtc = DateTime.UtcNow;
            NextRespawnUtc = DateTime.UtcNow.AddHours(12);
        }

        public bool ShouldRespawn() => IsCleared && DateTime.UtcNow >= NextRespawnUtc;

        public void Respawn()
        {
            IsCleared = false;
            ClearedAtUtc = null;
            NextRespawnUtc = DateTime.UtcNow.AddHours(12);
        }
    }
}
