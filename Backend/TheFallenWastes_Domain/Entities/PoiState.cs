using System;
using System.Collections.Generic;
using System.Linq;

namespace TheFallenWastes_Domain.Entities
{
    public class PoiState
    {
        public Guid Id { get; private set; }
        public string PoiId { get; private set; }
        public bool IsCleared { get; private set; }
        public DateTime? ClearedAtUtc { get; private set; }
        public DateTime NextRespawnUtc { get; private set; }
        public string? NpcUnitsJson { get; private set; }
        public string? LootItemsJson { get; private set; }
        public bool IsInitialized { get; private set; }
        public int GenerationSeed { get; private set; }
        public bool IsRelocating { get; private set; }
        public DateTime? RelocatingAtUtc { get; private set; }

        private PoiState() { }

        public PoiState(string poiId)
        {
            Id = Guid.NewGuid();
            PoiId = poiId;
            IsCleared = false;
            NextRespawnUtc = DateTime.UtcNow.AddHours(12);
        }

        public void Initialize(string npcUnitsJson, string lootItemsJson)
        {
            NpcUnitsJson = npcUnitsJson;
            LootItemsJson = lootItemsJson;
            IsInitialized = true;
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
            IsInitialized = false;
            NpcUnitsJson = null;
            LootItemsJson = null;
            ClearedAtUtc = null;
            NextRespawnUtc = DateTime.UtcNow.AddHours(12);
        }
        public void TriggerRelocation()
        {
            GenerationSeed += 1;
            IsCleared = false;
            IsInitialized = false;
            NpcUnitsJson = null;
            LootItemsJson = null;
            ClearedAtUtc = null;
            IsRelocating = true;
            RelocatingAtUtc = DateTime.UtcNow;
            NextRespawnUtc = DateTime.UtcNow.AddHours(12);
        }

        public void CompleteRelocation()
        {
            IsRelocating = false;
            RelocatingAtUtc = null;
        }
        public void ApplyNpcLosses(Dictionary<string, int> losses)
        {
            if (string.IsNullOrEmpty(NpcUnitsJson)) return;
            var units = System.Text.Json.JsonSerializer
                .Deserialize<Dictionary<string, int>>(NpcUnitsJson)
                ?? new Dictionary<string, int>();
            foreach (var kvp in losses)
            {
                if (units.ContainsKey(kvp.Key))
                {
                    units[kvp.Key] = Math.Max(0, units[kvp.Key] - kvp.Value);
                    if (units[kvp.Key] == 0) units.Remove(kvp.Key);
                }
            }
            NpcUnitsJson = System.Text.Json.JsonSerializer.Serialize(units);
            if (!units.Any())
            {
                IsCleared = true;
                ClearedAtUtc = DateTime.UtcNow;
                NextRespawnUtc = DateTime.UtcNow.AddHours(12);
            }
        }

        public void RemoveLootItem(string item)
        {
            if (string.IsNullOrEmpty(LootItemsJson)) return;
            var items = System.Text.Json.JsonSerializer
                .Deserialize<List<string>>(LootItemsJson) ?? new List<string>();
            items.Remove(item);
            LootItemsJson = System.Text.Json.JsonSerializer.Serialize(items);
        }
    }
}
