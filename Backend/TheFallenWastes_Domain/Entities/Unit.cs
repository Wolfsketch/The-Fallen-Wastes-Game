using TheFallenWastes_Domain.Enums;

namespace TheFallenWastes_Domain.Entities
{
    public class Unit
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Role { get; private set; }

        public UnitCategory UnitType { get; private set; }
        public ProductionFacility Facility { get; private set; }
        public CombatDamageType AttackType { get; private set; }

        public string IconKey { get; private set; }

        public int AttackPower { get; private set; }
        public int DefenseVsBallistic { get; private set; }
        public int DefenseVsImpact { get; private set; }
        public int DefenseVsEnergy { get; private set; }

        public int Speed { get; private set; }
        public int CapacityCost { get; private set; }
        public int CarryCapacity { get; private set; }
        public int Upkeep { get; private set; }
        public int BuildTimeSeconds { get; private set; }

        public UnitCost Cost { get; private set; }

        public Unit(
            string name,
            string description,
            string role,
            UnitCategory unitType,
            ProductionFacility facility,
            CombatDamageType attackType,
            string iconKey,
            int attackPower,
            int defenseVsBallistic,
            int defenseVsImpact,
            int defenseVsEnergy,
            int speed,
            int capacityCost,
            int carryCapacity,
            int upkeep,
            int buildTimeSeconds,
            UnitCost cost)
        {
            Name = name;
            Description = description;
            Role = role;
            UnitType = unitType;
            Facility = facility;
            AttackType = attackType;
            IconKey = iconKey;

            AttackPower = attackPower;
            DefenseVsBallistic = defenseVsBallistic;
            DefenseVsImpact = defenseVsImpact;
            DefenseVsEnergy = defenseVsEnergy;

            Speed = speed;
            CapacityCost = capacityCost;
            CarryCapacity = carryCapacity;
            Upkeep = upkeep;
            BuildTimeSeconds = buildTimeSeconds;

            Cost = cost ?? new UnitCost();
        }
    }
}