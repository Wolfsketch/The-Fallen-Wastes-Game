using System;

namespace TheFallenWastes_Domain.Entities
{
    public class SalvageItem
    {
        public Guid Id { get; private set; }

        /// <summary>
        /// Settlement-bound salvage inventory.
        /// Each item belongs to one settlement.
        /// </summary>
        public Guid SettlementId { get; private set; }

        /// <summary>
        /// Gets the unique identifier for the associated settlement salvage inventory.
        /// </summary>
        public Guid SettlementSalvageInventoryId { get; private set; }

        /// <summary>
        /// Canonical item key, for example:
        /// "broken_servo_arm", "military_datacore", "cracked_power_cell"
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// UI display name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Optional item description.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Source category such as Event, POI, Outpost, Quest.
        /// </summary>
        public string SourceType { get; private set; }

        /// <summary>
        /// Rarity such as Common, Uncommon, Rare, Epic, Legendary.
        /// </summary>
        public string Rarity { get; private set; }

        /// <summary>
        /// Stack quantity in this settlement inventory.
        /// </summary>
        public int Quantity { get; private set; }

        /// <summary>
        /// Minimum Tech Salvager level required to process this item.
        /// </summary>
        public int RequiredTechSalvagerLevel { get; private set; }

        /// <summary>
        /// Base salvage time in seconds for one item.
        /// </summary>
        public int BaseSalvageTimeSeconds { get; private set; }

        /// <summary>
        /// Expected outputs when salvaged.
        /// These are blueprint values and can later be overridden by systems.
        /// </summary>
        public int RareTechYield { get; private set; }
        public int ResearchDataYield { get; private set; }

        /// <summary>
        /// Optional special output key, for example:
        /// "Recovered Circuit", "Weapon Schematic", "Reactor Core"
        /// </summary>
        public string? SpecialOutputKey { get; private set; }

        public DateTime AcquiredAtUtc { get; private set; }

        private SalvageItem()
        {
            Key = string.Empty;
            Name = string.Empty;
            Description = string.Empty;
            SourceType = string.Empty;
            Rarity = string.Empty;
        }

        public SalvageItem(
            Guid settlementId,
            string key,
            string name,
            string description,
            string sourceType,
            string rarity,
            int quantity,
            int requiredTechSalvagerLevel,
            int baseSalvageTimeSeconds,
            int rareTechYield,
            int researchDataYield,
            string? specialOutputKey = null)
        {
            if (settlementId == Guid.Empty)
                throw new ArgumentException("SettlementId cannot be empty.", nameof(settlementId));

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Item key cannot be empty.", nameof(key));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Item name cannot be empty.", nameof(name));

            if (string.IsNullOrWhiteSpace(sourceType))
                throw new ArgumentException("SourceType cannot be empty.", nameof(sourceType));

            if (string.IsNullOrWhiteSpace(rarity))
                throw new ArgumentException("Rarity cannot be empty.", nameof(rarity));

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

            if (requiredTechSalvagerLevel < 0)
                throw new ArgumentException("RequiredTechSalvagerLevel cannot be negative.", nameof(requiredTechSalvagerLevel));

            if (baseSalvageTimeSeconds <= 0)
                throw new ArgumentException("BaseSalvageTimeSeconds must be greater than zero.", nameof(baseSalvageTimeSeconds));

            if (rareTechYield < 0)
                throw new ArgumentException("RareTechYield cannot be negative.", nameof(rareTechYield));

            if (researchDataYield < 0)
                throw new ArgumentException("ResearchDataYield cannot be negative.", nameof(researchDataYield));

            Id = Guid.NewGuid();
            SettlementId = settlementId;
            Key = key.Trim().ToLowerInvariant();
            Name = name.Trim();
            Description = description?.Trim() ?? string.Empty;
            SourceType = sourceType.Trim();
            Rarity = rarity.Trim();
            Quantity = quantity;
            RequiredTechSalvagerLevel = requiredTechSalvagerLevel;
            BaseSalvageTimeSeconds = baseSalvageTimeSeconds;
            RareTechYield = rareTechYield;
            ResearchDataYield = researchDataYield;
            SpecialOutputKey = string.IsNullOrWhiteSpace(specialOutputKey) ? null : specialOutputKey.Trim();
            AcquiredAtUtc = DateTime.UtcNow;
        }

        public void AddQuantity(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

            Quantity += amount;
        }

        public void RemoveQuantity(int amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

            if (amount > Quantity)
                throw new InvalidOperationException("Not enough quantity available.");

            Quantity -= amount;
        }

        public bool CanBeProcessedAtLevel(int techSalvagerLevel)
        {
            return techSalvagerLevel >= RequiredTechSalvagerLevel;
        }

        public int GetTotalRareTechYield(int amount)
        {
            if (amount <= 0)
                return 0;

            return RareTechYield * amount;
        }

        public int GetTotalResearchDataYield(int amount)
        {
            if (amount <= 0)
                return 0;

            return ResearchDataYield * amount;
        }

        public int GetTotalSalvageTimeSeconds(int amount)
        {
            if (amount <= 0)
                return 0;

            return BaseSalvageTimeSeconds * amount;
        }
    }
}