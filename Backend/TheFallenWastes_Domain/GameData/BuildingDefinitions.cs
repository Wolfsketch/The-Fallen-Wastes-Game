using System;
using System.Collections.Generic;
using System.Linq;
using TheFallenWastes_Domain.Enums;

namespace TheFallenWastes_Domain.GameData
{
    public static class BuildingDefinitions
    {
        public const int MaxBuildingLevel = 30;

        /// <summary>
        /// Buildings that start at level 1 for a newly founded settlement.
        /// Everything not listed here starts at level 0.
        /// </summary>
        public static readonly Dictionary<BuildingType, int> StarterBuildingLevels = new()
        {
            { BuildingType.HeadQuarter, 1 },
            { BuildingType.Shelter, 1 },

            { BuildingType.FoodSilo, 1 },
            { BuildingType.FuelDepot, 1 },
            { BuildingType.PowerBank, 1 },
            { BuildingType.ScrapVault, 1 },
            { BuildingType.WaterTank, 1 },
            { BuildingType.TechVault, 1 }
        };

        /// <summary>
        /// Convenience list for older code paths that only expect the level 1 starter buildings.
        /// </summary>
        public static readonly BuildingType[] StarterBuildings = StarterBuildingLevels.Keys.ToArray();

        public static int GetStartingLevel(BuildingType type)
        {
            return StarterBuildingLevels.TryGetValue(type, out var level) ? level : 0;
        }

        /// <summary>
        /// Future-feature buildings exist in the design but are not buildable yet.
        /// </summary>
        public static bool IsFutureFeature(BuildingType type)
        {
            return type == BuildingType.WatchTower;
        }

        /// <summary>
        /// Returns true when the building is allowed to be upgraded/downgraded by players.
        /// </summary>
        public static bool IsBuildable(BuildingType type)
        {
            return !IsFutureFeature(type);
        }

        /// <summary>
        /// Get prerequisites for a building type.
        /// Returns null if no prerequisites (always available).
        /// </summary>
        public static List<BuildingPrerequisite>? GetPrerequisites(BuildingType type)
        {
            return type switch
            {
                // CENTER / CORE
                BuildingType.HeadQuarter => null,
                BuildingType.Shelter => null,

                // RESOURCE PRODUCTION — available from the start, but begin at level 0
                BuildingType.FarmDome => null,
                BuildingType.FuelRefinery => null,
                BuildingType.ScrapForge => null,
                BuildingType.WaterPurifier => null,
                BuildingType.SolarArray => null,

                // STORAGE — begin at level 1
                BuildingType.FoodSilo => null,
                BuildingType.FuelDepot => null,
                BuildingType.PowerBank => null,
                BuildingType.ScrapVault => null,
                BuildingType.WaterTank => null,
                BuildingType.TechVault => null,

                // MILITARY
                BuildingType.Barracks => new()
                {
                    new(BuildingType.HeadQuarter, 2),
                    new(BuildingType.Shelter, 3),
                    new(BuildingType.FarmDome, 3),
                    new(BuildingType.FuelRefinery, 3),
                    new(BuildingType.ScrapForge, 3),
                    new(BuildingType.WaterPurifier, 3),
                    new(BuildingType.SolarArray, 3)
                },

                BuildingType.Garage => new()
                {
                    new(BuildingType.HeadQuarter, 4),
                    new(BuildingType.Shelter, 5),
                    new(BuildingType.FarmDome, 7),
                    new(BuildingType.FuelRefinery, 7),
                    new(BuildingType.ScrapForge, 7),
                    new(BuildingType.WaterPurifier, 7),
                    new(BuildingType.SolarArray, 7)
                },

                BuildingType.Workshop => new()
                {
                    new(BuildingType.HeadQuarter, 4),
                    new(BuildingType.Shelter, 7),
                    new(BuildingType.TechLab, 4),
                    new(BuildingType.FarmDome, 10),
                    new(BuildingType.FuelRefinery, 10),
                    new(BuildingType.ScrapForge, 10),
                    new(BuildingType.WaterPurifier, 10),
                    new(BuildingType.SolarArray, 10)
                },

                BuildingType.CommandCenter => new()
                {
                    new(BuildingType.HeadQuarter, 5),
                    new(BuildingType.Shelter, 8),
                    new(BuildingType.TechLab, 8),
                    new(BuildingType.FarmDome, 12),
                    new(BuildingType.FuelRefinery, 12),
                    new(BuildingType.ScrapForge, 12),
                    new(BuildingType.WaterPurifier, 12),
                    new(BuildingType.SolarArray, 12)
                },

                // DEFENSE
                BuildingType.PerimeterWall => new()
                {
                    new(BuildingType.HeadQuarter, 3),
                    new(BuildingType.Shelter, 2),
                    new(BuildingType.Barracks, 1)
                },

                // RESEARCH
                BuildingType.TechLab => new()
                {
                    new(BuildingType.HeadQuarter, 3),
                    new(BuildingType.Shelter, 4),
                    new(BuildingType.FarmDome, 5),
                    new(BuildingType.FuelRefinery, 5),
                    new(BuildingType.ScrapForge, 5),
                    new(BuildingType.WaterPurifier, 5),
                    new(BuildingType.SolarArray, 5)
                },

                BuildingType.TechSalvager => new()
                {
                    new(BuildingType.HeadQuarter, 4),
                    new(BuildingType.TechLab, 2),
                    new(BuildingType.ScrapForge, 6),
                    new(BuildingType.SolarArray, 6)
                },

                // CIVIC / EXPANSION
                BuildingType.CouncilHall => new()
                {
                    new(BuildingType.HeadQuarter, 6),
                    new(BuildingType.Shelter, 8),
                    new(BuildingType.TechLab, 4)
                },

                // SPECIAL STORAGE
                BuildingType.RaidVault => new()
                {
                    new(BuildingType.TechLab, 3),
                    new(BuildingType.TechVault, 5)
                },

                // FUTURE FEATURE
                BuildingType.WatchTower => new()
                {
                    new(BuildingType.HeadQuarter, 999)
                },

                _ => null
            };
        }

        /// <summary>
        /// Check if prerequisites are met for a building type.
        /// </summary>
        public static bool ArePrerequisitesMet(BuildingType type, Dictionary<BuildingType, int> currentLevels)
        {
            if (IsFutureFeature(type))
                return false;

            var prereqs = GetPrerequisites(type);
            if (prereqs == null)
                return true;

            foreach (var prereq in prereqs)
            {
                currentLevels.TryGetValue(prereq.RequiredType, out int level);
                if (level < prereq.RequiredLevel)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Get the resource cost to upgrade a building to the given level.
        /// targetLevel = the level being upgraded TO.
        /// </summary>
        public static BuildingCost GetUpgradeCost(BuildingType type, int targetLevel)
        {
            if (targetLevel <= 0)
                return new BuildingCost();

            var baseCost = GetBaseCost(type);
            double multiplier = Math.Pow(targetLevel, 1.55);

            return new BuildingCost
            {
                Water = (int)Math.Round(baseCost.Water * multiplier),
                Food = (int)Math.Round(baseCost.Food * multiplier),
                Scrap = (int)Math.Round(baseCost.Scrap * multiplier),
                Fuel = (int)Math.Round(baseCost.Fuel * multiplier),
                Energy = (int)Math.Round(baseCost.Energy * multiplier),
                RareTech = (int)Math.Round(baseCost.RareTech * multiplier)
            };
        }

        /// <summary>
        /// Build time in seconds for upgrading to the given level, before HeadQuarter speed reduction.
        /// </summary>
        public static int GetBuildTimeSeconds(BuildingType type, int targetLevel)
        {
            if (targetLevel <= 0)
                return 0;

            int baseSeconds = GetBaseBuildTime(type);
            return (int)Math.Round(baseSeconds * Math.Pow(targetLevel, 1.42));
        }

        /// <summary>
        /// Returns a multiplier that reduces building time based on HeadQuarter level.
        /// Example: 1.00 = no reduction, 0.80 = 20% faster.
        /// </summary>
        public static double GetHeadQuarterBuildTimeMultiplier(int headQuarterLevel)
        {
            if (headQuarterLevel <= 0)
                return 1.0;

            // Roughly 1.5% faster per HQ level, capped at 45% reduction.
            double reduction = Math.Min(0.45, headQuarterLevel * 0.015);
            return 1.0 - reduction;
        }

        /// <summary>
        /// Hourly resource production for a building at the given level.
        /// Resource buildings start at level 0 and produce nothing there.
        /// Level 1 is based on your intended ~23/h starting direction.
        /// </summary>
        public static ResourceProduction GetHourlyProduction(BuildingType type, int level)
        {
            if (level <= 0)
                return new ResourceProduction();

            double production = 23 * level * Math.Pow(1.12, level - 1);

            return type switch
            {
                BuildingType.WaterPurifier => new ResourceProduction { Water = (int)Math.Round(production) },
                BuildingType.FarmDome => new ResourceProduction { Food = (int)Math.Round(production) },
                BuildingType.ScrapForge => new ResourceProduction { Scrap = (int)Math.Round(production) },
                BuildingType.FuelRefinery => new ResourceProduction { Fuel = (int)Math.Round(production) },
                BuildingType.SolarArray => new ResourceProduction { Energy = (int)Math.Round(production) },
                _ => new ResourceProduction()
            };
        }

        /// <summary>
        /// Total population capacity provided by Shelter at the given level.
        /// Level 1 = 200 free inhabitants as agreed.
        /// </summary>
        public static int GetPopulationBonus(BuildingType type, int level)
        {
            if (type != BuildingType.Shelter || level <= 0)
                return 0;

            return 200 + (35 * (level - 1)) + (6 * (level - 1) * (level - 1));
        }

        /// <summary>
        /// Population consumed by a building at the given level.
        /// This is total usage for the full level, not per-level delta.
        /// </summary>
        public static int GetPopulationUsage(BuildingType type, int level)
        {
            if (level <= 0)
                return 0;

            return type switch
            {
                BuildingType.HeadQuarter => level * 4,
                BuildingType.Shelter => 0,
                BuildingType.CouncilHall => level * 3,

                BuildingType.FarmDome => level * 2,
                BuildingType.FuelRefinery => level * 2,
                BuildingType.ScrapForge => level * 2,
                BuildingType.WaterPurifier => level * 2,
                BuildingType.SolarArray => level * 2,

                BuildingType.FoodSilo => 0,
                BuildingType.FuelDepot => 0,
                BuildingType.PowerBank => 0,
                BuildingType.ScrapVault => 0,
                BuildingType.WaterTank => 0,
                BuildingType.TechVault => 0,
                BuildingType.RaidVault => 0,

                BuildingType.Barracks => level * 3,
                BuildingType.Garage => level * 4,
                BuildingType.Workshop => level * 4,
                BuildingType.CommandCenter => level * 5,

                BuildingType.PerimeterWall => 0,
                BuildingType.WatchTower => 0,

                BuildingType.TechLab => level * 3,
                BuildingType.TechSalvager => level * 2,

                _ => 0
            };
        }

        /// <summary>
        /// Additional storage capacity granted by the building at the given level.
        /// Total returned capacity for the storage building itself, not the settlement base storage.
        /// </summary>
        public static int GetStorageBonus(BuildingType type, int level)
        {
            if (level <= 0)
                return 0;

            return type switch
            {
                BuildingType.WaterTank => 1000 + ((level - 1) * 900),
                BuildingType.FoodSilo => 1000 + ((level - 1) * 900),
                BuildingType.ScrapVault => 1000 + ((level - 1) * 900),
                BuildingType.FuelDepot => 1000 + ((level - 1) * 900),
                BuildingType.PowerBank => 1000 + ((level - 1) * 900),
                BuildingType.TechVault => 200 + ((level - 1) * 180),
                BuildingType.RaidVault => level >= 10 ? int.MaxValue : level * 100,
                _ => 0
            };
        }

        /// <summary>
        /// Defensive strength from defensive structures.
        /// </summary>
        public static int GetDefenseValue(BuildingType type, int level)
        {
            if (level <= 0)
                return 0;

            return type switch
            {
                BuildingType.PerimeterWall => (int)Math.Round(35 * level * Math.Pow(1.11, level - 1)),
                BuildingType.WatchTower => 0,
                _ => 0
            };
        }

        /// <summary>
        /// Settlement power contribution from the building at the given level.
        /// This is total power at the given level.
        /// </summary>
        public static int GetPowerValue(BuildingType type, int level)
        {
            if (level <= 0)
                return 0;

            return type switch
            {
                BuildingType.HeadQuarter => level * 20,
                BuildingType.Shelter => level * 14,
                BuildingType.CouncilHall => level * 18,

                BuildingType.FarmDome => level * 16,
                BuildingType.FuelRefinery => level * 16,
                BuildingType.ScrapForge => level * 16,
                BuildingType.WaterPurifier => level * 16,
                BuildingType.SolarArray => level * 16,

                BuildingType.FoodSilo => level * 4,
                BuildingType.FuelDepot => level * 4,
                BuildingType.PowerBank => level * 4,
                BuildingType.ScrapVault => level * 4,
                BuildingType.WaterTank => level * 4,
                BuildingType.TechVault => level * 6,
                BuildingType.RaidVault => level * 25,

                BuildingType.Barracks => level * 22,
                BuildingType.Garage => level * 24,
                BuildingType.Workshop => level * 24,
                BuildingType.CommandCenter => level * 28,

                BuildingType.PerimeterWall => level * 14,
                BuildingType.WatchTower => 0,

                BuildingType.TechLab => level * 22,
                BuildingType.TechSalvager => level * 18,

                _ => 0
            };
        }

        /// <summary>
        /// Percentage bonus to unit training speed from facility level.
        /// Returned as multiplier divisor source, for example:
        /// effectiveTime = baseTime / GetTrainingSpeedFactor(level)
        /// </summary>
        public static double GetTrainingSpeedFactor(BuildingType facilityType, int level)
        {
            if (level <= 0)
                return 1.0;

            return facilityType switch
            {
                BuildingType.Barracks or BuildingType.Garage or BuildingType.Workshop or BuildingType.CommandCenter
                    => 1.0 + (0.05 * (level - 1)),
                _ => 1.0
            };
        }

        public static string GetDisplayName(BuildingType type)
        {
            return type switch
            {
                BuildingType.HeadQuarter => "HeadQuarter",
                BuildingType.Shelter => "Shelter",
                BuildingType.CouncilHall => "Council Hall",

                BuildingType.FarmDome => "Farm Dome",
                BuildingType.FuelRefinery => "Fuel Refinery",
                BuildingType.ScrapForge => "Scrap Forge",
                BuildingType.WaterPurifier => "Water Purifier",
                BuildingType.SolarArray => "Solar Array",

                BuildingType.FoodSilo => "Food Silo",
                BuildingType.FuelDepot => "Fuel Depot",
                BuildingType.PowerBank => "Power Bank",
                BuildingType.ScrapVault => "Scrap Vault",
                BuildingType.WaterTank => "Water Tank",
                BuildingType.TechVault => "Tech Vault",
                BuildingType.RaidVault => "Relic Vault",

                BuildingType.Barracks => "Barracks",
                BuildingType.Garage => "Garage",
                BuildingType.Workshop => "Workshop",
                BuildingType.CommandCenter => "Command Center",

                BuildingType.PerimeterWall => "Perimeter Wall",
                BuildingType.WatchTower => "WatchTower",

                BuildingType.TechLab => "Tech Lab",
                BuildingType.TechSalvager => "Tech Salvager",

                _ => type.ToString()
            };
        }

        public static string GetDescription(BuildingType type)
        {
            return type switch
            {
                BuildingType.HeadQuarter => "Central command hub. Higher levels reduce construction time for all buildings.",
                BuildingType.Shelter => "Increases the total population cap of the settlement and determines how many residents are available.",
                BuildingType.CouncilHall => "Civic administration center for expansion, convoy-related systems and future settlement governance.",

                BuildingType.FarmDome => "Produces Food per hour for the settlement.",
                BuildingType.FuelRefinery => "Produces Fuel per hour for vehicles, production and logistics.",
                BuildingType.ScrapForge => "Produces Scrap per hour from reclaimed metal and debris.",
                BuildingType.WaterPurifier => "Produces Water per hour via filtration and purification.",
                BuildingType.SolarArray => "Produces Energy per hour from solar fields and battery grids.",

                BuildingType.FoodSilo => "Increases storage capacity for Food and provides settlement power.",
                BuildingType.FuelDepot => "Increases storage capacity for Fuel and provides settlement power.",
                BuildingType.PowerBank => "Increases storage capacity for Energy and provides settlement power.",
                BuildingType.ScrapVault => "Increases storage capacity for Scrap and provides settlement power.",
                BuildingType.WaterTank => "Increases storage capacity for Water and provides settlement power.",
                BuildingType.TechVault => "Increases storage capacity for RareTech and provides extra settlement power.",
                BuildingType.RaidVault => "Secured archive for RareTech reserves. Larger stock slows scout raids. Unlimited storage from level 10.",

                BuildingType.Barracks => "Unlocks and accelerates infantry training.",
                BuildingType.Garage => "Unlocks and accelerates vehicle training.",
                BuildingType.Workshop => "Unlocks and accelerates advanced and support unit training.",
                BuildingType.CommandCenter => "Unlocks command units and special operational units such as Convoy.",

                BuildingType.PerimeterWall => "Strengthens the defensive position of the settlement. Makes it harder for enemy Convoys to break through.",
                BuildingType.WatchTower => "Future feature. Not yet active or buildable.",

                BuildingType.TechLab => "Unlocks research and increases research speed.",
                BuildingType.TechSalvager => "Processes event and POI items into RareTech, components and research materials.",

                _ => "Unknown building"
            };
        }

        public static string GetCategory(BuildingType type)
        {
            return type switch
            {
                BuildingType.HeadQuarter or BuildingType.Shelter or BuildingType.CouncilHall
                    => "center",

                BuildingType.FarmDome or BuildingType.FuelRefinery or BuildingType.ScrapForge
                    or BuildingType.WaterPurifier or BuildingType.SolarArray
                    => "resource",

                BuildingType.FoodSilo or BuildingType.FuelDepot or BuildingType.PowerBank
                    or BuildingType.ScrapVault or BuildingType.WaterTank
                    => "storage",

                BuildingType.TechVault or BuildingType.RaidVault
                    => "special-storage",

                BuildingType.Barracks or BuildingType.Garage or BuildingType.Workshop or BuildingType.CommandCenter
                    => "military",

                BuildingType.PerimeterWall or BuildingType.WatchTower
                    => "defense",

                BuildingType.TechLab or BuildingType.TechSalvager
                    => "research",

                _ => "other"
            };
        }

        private static BuildingCost GetBaseCost(BuildingType type)
        {
            return type switch
            {
                // CENTER
                BuildingType.HeadQuarter => new BuildingCost
                {
                    Water = 70,
                    Food = 70,
                    Scrap = 140,
                    Fuel = 30,
                    Energy = 20
                },

                BuildingType.Shelter => new BuildingCost
                {
                    Water = 80,
                    Food = 70,
                    Scrap = 130,
                    Fuel = 25,
                    Energy = 10
                },

                BuildingType.CouncilHall => new BuildingCost
                {
                    Water = 120,
                    Food = 120,
                    Scrap = 220,
                    Fuel = 70,
                    Energy = 60,
                    RareTech = 10
                },

                // RESOURCE PRODUCTION
                BuildingType.FarmDome => new BuildingCost
                {
                    Water = 90,
                    Food = 30,
                    Scrap = 70,
                    Fuel = 20,
                    Energy = 15
                },

                BuildingType.FuelRefinery => new BuildingCost
                {
                    Water = 70,
                    Food = 40,
                    Scrap = 110,
                    Fuel = 20,
                    Energy = 25
                },

                BuildingType.ScrapForge => new BuildingCost
                {
                    Water = 50,
                    Food = 40,
                    Scrap = 90,
                    Fuel = 30,
                    Energy = 15
                },

                BuildingType.WaterPurifier => new BuildingCost
                {
                    Water = 40,
                    Food = 40,
                    Scrap = 90,
                    Fuel = 20,
                    Energy = 15
                },

                BuildingType.SolarArray => new BuildingCost
                {
                    Water = 30,
                    Food = 20,
                    Scrap = 120,
                    Fuel = 20,
                    Energy = 20
                },

                // STORAGE
                BuildingType.FoodSilo => new BuildingCost
                {
                    Food = 40,
                    Scrap = 90,
                    Fuel = 20
                },

                BuildingType.FuelDepot => new BuildingCost
                {
                    Scrap = 95,
                    Fuel = 20,
                    Energy = 15
                },

                BuildingType.PowerBank => new BuildingCost
                {
                    Scrap = 100,
                    Fuel = 20,
                    Energy = 20
                },

                BuildingType.ScrapVault => new BuildingCost
                {
                    Scrap = 110,
                    Fuel = 25
                },

                BuildingType.WaterTank => new BuildingCost
                {
                    Water = 40,
                    Scrap = 90,
                    Fuel = 20
                },

                BuildingType.TechVault => new BuildingCost
                {
                    Scrap = 140,
                    Fuel = 35,
                    Energy = 30,
                    RareTech = 5
                },

                BuildingType.RaidVault => new BuildingCost
                {
                    Scrap = 140,
                    Fuel = 35,
                    Energy = 30,
                    RareTech = 5
                },

                // MILITARY
                BuildingType.Barracks => new BuildingCost
                {
                    Water = 80,
                    Food = 100,
                    Scrap = 120,
                    Fuel = 35,
                    Energy = 20
                },

                BuildingType.Garage => new BuildingCost
                {
                    Scrap = 220,
                    Fuel = 120,
                    Energy = 50
                },

                BuildingType.Workshop => new BuildingCost
                {
                    Scrap = 240,
                    Fuel = 100,
                    Energy = 90,
                    RareTech = 15
                },

                BuildingType.CommandCenter => new BuildingCost
                {
                    Water = 120,
                    Food = 120,
                    Scrap = 240,
                    Fuel = 90,
                    Energy = 70,
                    RareTech = 20
                },

                // DEFENSE
                BuildingType.PerimeterWall => new BuildingCost
                {
                    Scrap = 130,
                    Fuel = 20
                },

                BuildingType.WatchTower => new BuildingCost
                {
                    Scrap = 0
                },

                // RESEARCH
                BuildingType.TechLab => new BuildingCost
                {
                    Scrap = 170,
                    Fuel = 60,
                    Energy = 90,
                    RareTech = 15
                },

                BuildingType.TechSalvager => new BuildingCost
                {
                    Scrap = 200,
                    Fuel = 80,
                    Energy = 70,
                    RareTech = 10
                },

                _ => new BuildingCost { Scrap = 100 }
            };
        }

        private static int GetBaseBuildTime(BuildingType type)
        {
            return type switch
            {
                // CENTER
                BuildingType.HeadQuarter => 240,
                BuildingType.Shelter => 180,
                BuildingType.CouncilHall => 420,

                // RESOURCE
                BuildingType.FarmDome => 120,
                BuildingType.FuelRefinery => 150,
                BuildingType.ScrapForge => 150,
                BuildingType.WaterPurifier => 120,
                BuildingType.SolarArray => 180,

                // STORAGE
                BuildingType.FoodSilo => 90,
                BuildingType.FuelDepot => 90,
                BuildingType.PowerBank => 90,
                BuildingType.ScrapVault => 90,
                BuildingType.WaterTank => 90,
                BuildingType.TechVault => 120,
                BuildingType.RaidVault => 120,

                // MILITARY
                BuildingType.Barracks => 240,
                BuildingType.Garage => 300,
                BuildingType.Workshop => 360,
                BuildingType.CommandCenter => 480,

                // DEFENSE
                BuildingType.PerimeterWall => 100,
                BuildingType.WatchTower => 0,

                // RESEARCH
                BuildingType.TechLab => 360,
                BuildingType.TechSalvager => 300,

                _ => 120
            };
        }
    }

    public class BuildingCost
    {
        public int Water { get; set; }
        public int Food { get; set; }
        public int Scrap { get; set; }
        public int Fuel { get; set; }
        public int Energy { get; set; }
        public int RareTech { get; set; }
    }

    public class ResourceProduction
    {
        public int Water { get; set; }
        public int Food { get; set; }
        public int Scrap { get; set; }
        public int Fuel { get; set; }
        public int Energy { get; set; }
        public int RareTech { get; set; }
    }

    public class BuildingPrerequisite
    {
        public BuildingType RequiredType { get; set; }
        public int RequiredLevel { get; set; }

        public BuildingPrerequisite(BuildingType type, int level)
        {
            RequiredType = type;
            RequiredLevel = level;
        }
    }
}