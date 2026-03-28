using System;
using System.Collections.Generic;
using System.Linq;
using TheFallenWastes_Domain.Enums;
using TheFallenWastes_Domain.GameData;
using TheFallenWastes_Domain.ValueObjects;

namespace TheFallenWastes_Domain.Entities
{
    public class Settlement
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public Guid PlayerId { get; private set; }

        public ResourceStock Resources { get; private set; }

        // Population system
        // PopulationCapacity is derived from Shelter level.
        public int PopulationCapacity => GetPopulationCapacityFromBuildings();

        private int _usedPopulation;
        public int UsedPopulation => _usedPopulation;

        public int AvailablePopulation => Math.Max(0, PopulationCapacity - UsedPopulation);

        // Morale for conquest system (0-100)
        public int Morale { get; private set; }

        // Last time resources were calculated
        public DateTime LastResourceTickUtc { get; private set; }

        // Buildings in this settlement
        public ICollection<Building> Buildings { get; private set; }

        // Unit inventory (unit name -> quantity)
        public Dictionary<string, int> UnitInventory { get; private set; }

        // Active training queue
        public ICollection<UnitTrainingQueueItem> TrainingQueue { get; private set; }

        // Storage capacities are derived from storage buildings
        public int WaterCapacity => GetStorageCapacity(BuildingType.WaterTank);
        public int FoodCapacity => GetStorageCapacity(BuildingType.FoodSilo);
        public int ScrapCapacity => GetStorageCapacity(BuildingType.ScrapVault);
        public int FuelCapacity => GetStorageCapacity(BuildingType.FuelDepot);
        public int EnergyCapacity => GetStorageCapacity(BuildingType.PowerBank);
        public int RareTechCapacity => GetStorageCapacity(BuildingType.TechVault);
        public int RaidVaultCapacity => GetStorageCapacity(BuildingType.RaidVault);

        // RareTech stored inside the Relic Vault (separate from the general resource pool)
        public int VaultRareTech { get; private set; }

        private Settlement()
        {
            Name = string.Empty;
            Resources = new ResourceStock();
            Buildings = new List<Building>();
            UnitInventory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            TrainingQueue = new List<UnitTrainingQueueItem>();
            _usedPopulation = 0;
            VaultRareTech = 0;
        }

        public Settlement(string name, Guid playerId)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Settlement name cannot be empty.", nameof(name));

            if (name.Length < 3)
                throw new ArgumentException("Settlement name must be at least 3 characters long.", nameof(name));

            if (playerId == Guid.Empty)
                throw new ArgumentException("PlayerId cannot be empty.", nameof(playerId));

            Id = Guid.NewGuid();
            Name = name;
            PlayerId = playerId;

            Resources = new ResourceStock(
                water: 750,
                food: 1000,
                scrap: 750,
                fuel: 400,
                energy: 300,
                rareTech: 0
            );

            Morale = 100;
            LastResourceTickUtc = DateTime.UtcNow;
            Buildings = new List<Building>();
            UnitInventory = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            TrainingQueue = new List<UnitTrainingQueueItem>();

            _usedPopulation = 0;
            VaultRareTech = 0;
            RecalculateUsedPopulation();
        }

        public void Rename(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Settlement name cannot be empty.", nameof(name));

            if (name.Length < 3)
                throw new ArgumentException("Settlement name must be at least 3 characters long.", nameof(name));

            Name = name;
        }

        /// <summary>
        /// Returns total population capacity derived from Shelter level.
        /// </summary>
        public int GetPopulationCapacityFromBuildings()
        {
            int shelterLevel = GetBuildingLevel(BuildingType.Shelter);
            return BuildingDefinitions.GetPopulationBonus(BuildingType.Shelter, shelterLevel);
        }

        /// <summary>
        /// Returns total population currently consumed by buildings.
        /// </summary>
        public int GetBuildingPopulationUsage()
        {
            if (Buildings == null || Buildings.Count == 0)
                return 0;

            return Buildings.Sum(b => BuildingDefinitions.GetPopulationUsage(b.Type, b.Level));
        }

        /// <summary>
        /// Returns total settlement power from all buildings.
        /// </summary>
        public int GetTotalBuildingPower()
        {
            if (Buildings == null || Buildings.Count == 0)
                return 0;

            return Buildings.Sum(b => BuildingDefinitions.GetPowerValue(b.Type, b.Level));
        }

        public int GetStorageCapacity(BuildingType storageType)
        {
            int level = GetBuildingLevel(storageType);
            return BuildingDefinitions.GetStorageBonus(storageType, level);
        }

        public int GetBuildingLevel(BuildingType type)
        {
            var building = Buildings?
                .Where(b => b.Type == type)
                .OrderByDescending(b => b.Level)
                .ThenByDescending(b => b.TargetLevel)
                .FirstOrDefault();

            if (building != null)
                return building.Level;

            return BuildingDefinitions.GetStartingLevel(type);
        }

        /// <summary>
        /// Recalculate total used population from buildings + owned units + queued units.
        /// Call this after loading or after major structural changes.
        /// </summary>
        public void RecalculateUsedPopulation()
        {
            int buildingPopulation = GetBuildingPopulationUsage();
            int unitPopulation = CalculateUnitPopulationUsage();
            int queuedPopulation = CalculateQueuedPopulationUsage();

            _usedPopulation = Math.Max(0, buildingPopulation + unitPopulation + queuedPopulation);
        }

        private int CalculateUnitPopulationUsage()
        {
            if (UnitInventory == null || UnitInventory.Count == 0)
                return 0;

            var unitDefinitions = TheFallenWastes_Domain.UnitFactory.UnitFactory.CreateStarterUnits()
                .ToDictionary(u => u.Name, u => u.CapacityCost, StringComparer.OrdinalIgnoreCase);

            int total = 0;

            foreach (var kvp in UnitInventory)
            {
                if (unitDefinitions.TryGetValue(kvp.Key, out var popCost))
                {
                    total += kvp.Value * popCost;
                }
            }

            return total;
        }

        private int CalculateQueuedPopulationUsage()
        {
            if (TrainingQueue == null || TrainingQueue.Count == 0)
                return 0;

            return TrainingQueue
                .Where(q => !q.IsCompleted)
                .Sum(q => q.GetTotalPopulationCost());
        }

        /// <summary>
        /// Add resources but cap at each resource's individual storage capacity.
        /// </summary>
        public void AddResourcesCapped(int water, int food, int scrap, int fuel, int energy, int rareTech)
        {
            int addW = Math.Min(water, Math.Max(0, WaterCapacity - Resources.Water));
            int addF = Math.Min(food, Math.Max(0, FoodCapacity - Resources.Food));
            int addS = Math.Min(scrap, Math.Max(0, ScrapCapacity - Resources.Scrap));
            int addFu = Math.Min(fuel, Math.Max(0, FuelCapacity - Resources.Fuel));
            int addE = Math.Min(energy, Math.Max(0, EnergyCapacity - Resources.Energy));
            int addR = Math.Min(rareTech, Math.Max(0, RareTechCapacity - Resources.RareTech));

            Resources.Add(
                water: addW,
                food: addF,
                scrap: addS,
                fuel: addFu,
                energy: addE,
                rareTech: addR
            );
        }

        /// <summary>
        /// Legacy overload — still works but uses per-resource caps instead of single cap.
        /// </summary>
        public void AddResourcesCapped(int water, int food, int scrap, int fuel, int energy, int rareTech, int storageCap)
        {
            AddResourcesCapped(water, food, scrap, fuel, energy, rareTech);
        }

        /// <summary>
        /// Add units to inventory and track population usage.
        /// </summary>
        public void AddUnits(string unitName, int quantity, int capacityCost)
        {
            if (string.IsNullOrWhiteSpace(unitName))
                throw new ArgumentException("Unit name cannot be empty.", nameof(unitName));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive.", nameof(quantity));

            RecalculateUsedPopulation();

            int requiredPopulation = quantity * capacityCost;
            if (AvailablePopulation < requiredPopulation)
            {
                throw new InvalidOperationException(
                    $"Not enough population. Required: {requiredPopulation}, Available: {AvailablePopulation}");
            }

            if (UnitInventory.ContainsKey(unitName))
                UnitInventory[unitName] += quantity;
            else
                UnitInventory[unitName] = quantity;

            RecalculateUsedPopulation();
        }

        /// <summary>
        /// Remove units from inventory and free population.
        /// </summary>
        public void RemoveUnits(string unitName, int quantity, int capacityCost)
        {
            if (string.IsNullOrWhiteSpace(unitName))
                throw new ArgumentException("Unit name cannot be empty.", nameof(unitName));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive.", nameof(quantity));

            if (!UnitInventory.ContainsKey(unitName))
                throw new InvalidOperationException($"No units of type {unitName} available.");

            if (UnitInventory[unitName] < quantity)
                throw new InvalidOperationException($"Not enough {unitName} units available.");

            UnitInventory[unitName] -= quantity;

            if (UnitInventory[unitName] == 0)
                UnitInventory.Remove(unitName);

            RecalculateUsedPopulation();
        }

        public UnitTrainingQueueItem QueueUnitTraining(string unitName, int quantity, int capacityCostPerUnit, int durationSeconds)
        {
            if (string.IsNullOrWhiteSpace(unitName))
                throw new ArgumentException("Unit name cannot be empty.", nameof(unitName));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be positive.", nameof(quantity));

            if (capacityCostPerUnit < 0)
                throw new ArgumentException("Population cost cannot be negative.", nameof(capacityCostPerUnit));

            if (durationSeconds <= 0)
                throw new ArgumentException("Duration must be greater than zero.", nameof(durationSeconds));

            RecalculateUsedPopulation();

            int requiredPopulation = quantity * capacityCostPerUnit;
            if (AvailablePopulation < requiredPopulation)
            {
                throw new InvalidOperationException(
                    $"Not enough population. Required: {requiredPopulation}, Available: {AvailablePopulation}");
            }

            var queueItem = new UnitTrainingQueueItem(
                Id,
                unitName,
                quantity,
                capacityCostPerUnit,
                durationSeconds
            );

            TrainingQueue.Add(queueItem);
            RecalculateUsedPopulation();

            return queueItem;
        }

        public int GetUnitCount(string unitName)
        {
            if (string.IsNullOrWhiteSpace(unitName))
                return 0;

            return UnitInventory.ContainsKey(unitName) ? UnitInventory[unitName] : 0;
        }

        /// <summary>
        /// Update the last resource tick timestamp.
        /// </summary>
        public void UpdateLastTick(DateTime utcNow)
        {
            LastResourceTickUtc = utcNow;
        }

        /// <summary>
        /// Reduce morale after a successful attack.
        /// </summary>
        public void ReduceMorale(int amount)
        {
            Morale = Math.Max(0, Morale - amount);
        }

        /// <summary>
        /// Regenerate morale (called on tick).
        /// </summary>
        public void RegenerateMorale(int amount)
        {
            Morale = Math.Min(100, Morale + amount);
        }

        /// <summary>
        /// Ensures resources never exceed current storage capacities.
        /// Useful after downgrades or migrations.
        /// </summary>
        public void CapResourcesToCurrentStorage()
        {
            Resources.CapToStorage(
                WaterCapacity,
                FoodCapacity,
                ScrapCapacity,
                FuelCapacity,
                EnergyCapacity,
                RareTechCapacity
            );
        }

        /// <summary>
        /// Moves RareTech from the general resource pool into the Relic Vault.
        /// </summary>
        public (int deposited, string? error) DepositToVault(int amount)
        {
            if (amount <= 0) return (0, "Amount must be positive.");
            if (Resources.RareTech < amount)
                return (0, "Not enough RareTech in your resource pool.");

            int capacity = RaidVaultCapacity;
            int space = capacity == int.MaxValue
                ? int.MaxValue
                : Math.Max(0, capacity - VaultRareTech);

            if (space <= 0) return (0, "Vault is full.");

            int actual = Math.Min(amount, space == int.MaxValue ? amount : space);
            Resources.Spend(0, 0, 0, 0, 0, actual);
            VaultRareTech += actual;
            return (actual, null);
        }

        /// <summary>
        /// Moves RareTech from the Relic Vault back into the general resource pool.
        /// </summary>
        public void WithdrawFromVault(int amount)
        {
            int actual = Math.Min(amount, VaultRareTech);
            VaultRareTech -= actual;
            Resources.Add(0, 0, 0, 0, 0, actual);
        }

        /// <summary>
        /// Returns how much RareTech was stolen.
        /// Attacker wins if attackerRareTech > defenderRaidVaultStock.
        /// Defender loses all their RaidVault stock.
        /// Returns 0 if attacker has less RT than defender (scout fails silently for defender).
        /// </summary>
        public int TryScoutRaidVault(int attackerRareTech, out bool defenderNotified)
        {
            // Vault stock IS the at-risk amount — general pool is not touched
            int atRisk = VaultRareTech;

            if (attackerRareTech > atRisk)
            {
                // Attacker wins: steal all vault RT from defender
                VaultRareTech -= atRisk;
                defenderNotified = true;
                return atRisk;
            }
            else
            {
                // Attacker loses: nothing happens to defender
                defenderNotified = false;
                return 0;
            }
        }
    }
}