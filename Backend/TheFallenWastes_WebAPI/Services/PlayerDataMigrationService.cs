using System;
using System.Collections.Generic;
using System.Linq;
using TheFallenWastes_Domain.Entities;
using TheFallenWastes_Domain.GameData;
using TheFallenWastes_Infrastructure;

namespace TheFallenWastes_Application
{
    public class PlayerDataMigrationService
    {
        public const int CurrentDataVersion = 2;

        public bool MigratePlayer(Player player, Settlement? settlement, GameDbContext db)
        {
            bool changed = false;

            if (player.DataVersion < 1)
            {
                player.SetDataVersion(1);
                changed = true;
            }

            if (player.DataVersion < 2)
            {
                if (settlement != null)
                {
                    // Ensure collections are initialized for old data
                    if (settlement.Buildings == null)
                    {
                        typeof(Settlement)
                            .GetProperty(nameof(Settlement.Buildings))?
                            .SetValue(settlement, new List<Building>());
                        changed = true;
                    }

                    if (settlement.UnitInventory == null)
                    {
                        typeof(Settlement)
                            .GetProperty(nameof(Settlement.UnitInventory))?
                            .SetValue(settlement, new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase));
                        changed = true;
                    }

                    // Ensure starter buildings exist
                    var existingTypes = settlement.Buildings
                        .Select(b => b.Type)
                        .ToHashSet();

                    foreach (var starterType in BuildingDefinitions.StarterBuildings)
                    {
                        if (!existingTypes.Contains(starterType))
                        {
                            var building = Building.CreateAtLevel(settlement.Id, starterType, 1);
                            db.Buildings.Add(building);
                            settlement.Buildings.Add(building);
                            changed = true;
                        }
                    }

                    // Safety fixes for old settlements
                    if (settlement.Morale <= 0)
                    {
                        settlement.RegenerateMorale(100);
                        changed = true;
                    }

                    if (settlement.LastResourceTickUtc == default)
                    {
                        settlement.UpdateLastTick(DateTime.UtcNow);
                        changed = true;
                    }

                    // Rebuild used population from buildings + unit inventory
                    settlement.RecalculateUsedPopulation();
                    changed = true;

                    // Ensure old data cannot exceed current storage rules
                    settlement.CapResourcesToCurrentStorage();
                    changed = true;
                }

                player.SetDataVersion(2);
                changed = true;
            }

            return changed;
        }
    }
}