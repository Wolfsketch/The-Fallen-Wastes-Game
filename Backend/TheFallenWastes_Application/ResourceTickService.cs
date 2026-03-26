using System;
using System.Collections.Generic;
using System.Linq;
using TheFallenWastes_Domain.Entities;
using TheFallenWastes_Domain.GameData;

namespace TheFallenWastes_Application.Services
{
    public static class ResourceTickService
    {
        /// <summary>
        /// Calculate and apply resource production since the last tick.
        /// Call this whenever you load a settlement (lazy tick).
        /// </summary>
        public static void ApplyResourceTick(Settlement settlement, IEnumerable<Building> buildings)
        {
            var now = DateTime.UtcNow;
            var lastTick = settlement.LastResourceTickUtc;
            var elapsed = now - lastTick;

            if (elapsed.TotalSeconds < 1) return;

            double hours = elapsed.TotalHours;

            // Sum production from all completed buildings
            int water = 0, food = 0, scrap = 0, fuel = 0, energy = 0, rareTech = 0;

            foreach (var building in buildings)
            {
                if (building.Level <= 0) continue;

                // Check if construction finished during this tick period
                building.TryCompleteConstruction();

                var prod = BuildingDefinitions.GetHourlyProduction(building.Type, building.Level);
                water += prod.Water;
                food += prod.Food;
                scrap += prod.Scrap;
                fuel += prod.Fuel;
                energy += prod.Energy;
                rareTech += prod.RareTech;
            }

            // Apply production * hours elapsed
            int addWater = (int)(water * hours);
            int addFood = (int)(food * hours);
            int addScrap = (int)(scrap * hours);
            int addFuel = (int)(fuel * hours);
            int addEnergy = (int)(energy * hours);
            int addRareTech = (int)(rareTech * hours);

            // Calculate storage cap from warehouses
            int storageCap = 2000; // base capacity
            foreach (var building in buildings)
            {
                storageCap += BuildingDefinitions.GetStorageBonus(building.Type, building.Level);
            }

            // Add resources capped at storage limit
            settlement.AddResourcesCapped(addWater, addFood, addScrap, addFuel, addEnergy, addRareTech, storageCap);
            settlement.UpdateLastTick(now);
        }

        /// <summary>
        /// Get total hourly production rates for display.
        /// </summary>
        public static ResourceProduction GetTotalProduction(IEnumerable<Building> buildings)
        {
            var total = new ResourceProduction();
            foreach (var building in buildings)
            {
                if (building.Level <= 0) continue;
                var prod = BuildingDefinitions.GetHourlyProduction(building.Type, building.Level);
                total.Water += prod.Water;
                total.Food += prod.Food;
                total.Scrap += prod.Scrap;
                total.Fuel += prod.Fuel;
                total.Energy += prod.Energy;
                total.RareTech += prod.RareTech;
            }
            return total;
        }
    }
}