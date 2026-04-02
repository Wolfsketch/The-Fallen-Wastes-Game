using System;
using System.Collections.Generic;
using System.Linq;

namespace TheFallenWastes_Domain.Entities
{
    public class PoiState
    {
        /// <summary>Respawn timer in minutes. Configurable constant (default: 15 minutes).</summary>
        public const int RespawnMinutes = 15;

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
            NextRespawnUtc = DateTime.UtcNow.AddMinutes(RespawnMinutes);
        }

        public void Initialize(string npcUnitsJson, string lootItemsJson)
        {
            NpcUnitsJson = npcUnitsJson;
            LootItemsJson = lootItemsJson;
            IsInitialized = true;
        }

        public void MarkCleared()
        {
            // Guard: only clear when no loot remains
            var loot = string.IsNullOrEmpty(LootItemsJson)
                ? new List<string>()
                : System.Text.Json.JsonSerializer.Deserialize<List<string>>(LootItemsJson) ?? new List<string>();
            if (loot.Any()) return;

            IsCleared = true;
            ClearedAtUtc = DateTime.UtcNow;
            NextRespawnUtc = DateTime.UtcNow.AddMinutes(RespawnMinutes);
        }

        /// <summary>Resets the cleared flag when loot is still present (heals inconsistent DB state).</summary>
        public void UnClear()
        {
            IsCleared = false;
            ClearedAtUtc = null;
        }

        public bool ShouldRespawn() => IsCleared && DateTime.UtcNow >= NextRespawnUtc;

        public void Respawn()
        {
            IsCleared = false;
            IsInitialized = false;
            NpcUnitsJson = null;
            LootItemsJson = null;
            ClearedAtUtc = null;
            NextRespawnUtc = DateTime.UtcNow.AddMinutes(RespawnMinutes);
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
            NextRespawnUtc = DateTime.UtcNow.AddMinutes(RespawnMinutes);
        }

        /// <summary>
        /// Begins a 15-minute relocation warning window.
        /// Content is still visible but no new operations are allowed.
        /// </summary>
        public void StartRelocationWarning(int warningMinutes = 15)
        {
            IsRelocating = true;
            RelocatingAtUtc = DateTime.UtcNow.AddMinutes(warningMinutes);
        }

        /// <summary>
        /// Called after the warning window expires.
        /// Resets content, increments seed, and ends the relocating state.
        /// </summary>
        public void FinalizeRelocation()
        {
            GenerationSeed += 1;
            IsCleared = false;
            IsInitialized = false;
            NpcUnitsJson = null;
            LootItemsJson = null;
            ClearedAtUtc = null;
            IsRelocating = false;
            RelocatingAtUtc = null;
            NextRespawnUtc = DateTime.UtcNow.AddMinutes(RespawnMinutes);
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
                // Only mark cleared if there is also no loot left
                var loot = string.IsNullOrEmpty(LootItemsJson)
                    ? new System.Collections.Generic.List<string>()
                    : System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<string>>(LootItemsJson)
                      ?? new System.Collections.Generic.List<string>();
                if (!loot.Any())
                {
                    IsCleared = true;
                    ClearedAtUtc = DateTime.UtcNow;
                    NextRespawnUtc = DateTime.UtcNow.AddMinutes(RespawnMinutes);
                }
            }
        }

        public void RemoveLootItem(string item)
        {
            if (string.IsNullOrEmpty(LootItemsJson)) return;
            var items = System.Text.Json.JsonSerializer
                .Deserialize<System.Collections.Generic.List<string>>(LootItemsJson) ?? new System.Collections.Generic.List<string>();
            items.Remove(item);
            LootItemsJson = System.Text.Json.JsonSerializer.Serialize(items);

            // If all loot is gone and all NPCs are gone → mark cleared
            if (!items.Any())
            {
                var npcs = string.IsNullOrEmpty(NpcUnitsJson)
                    ? new System.Collections.Generic.Dictionary<string, int>()
                    : System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, int>>(NpcUnitsJson)
                      ?? new System.Collections.Generic.Dictionary<string, int>();
                if (!npcs.Any())
                {
                    IsCleared = true;
                    ClearedAtUtc = DateTime.UtcNow;
                    NextRespawnUtc = DateTime.UtcNow.AddMinutes(RespawnMinutes);
                }
            }
        }
    }
}
