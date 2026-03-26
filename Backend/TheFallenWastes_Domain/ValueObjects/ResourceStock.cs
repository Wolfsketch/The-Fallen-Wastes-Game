using System;
using TheFallenWastes_Domain.Entities;

namespace TheFallenWastes_Domain.ValueObjects
{
    public class ResourceStock
    {
        public int Water { get; private set; }
        public int Food { get; private set; }
        public int Scrap { get; private set; }
        public int Fuel { get; private set; }
        public int Energy { get; private set; }
        public int RareTech { get; private set; }

        public ResourceStock(
            int water = 0,
            int food = 0,
            int scrap = 0,
            int fuel = 0,
            int energy = 0,
            int rareTech = 0)
        {
            if (water < 0 || food < 0 || scrap < 0 || fuel < 0 || energy < 0 || rareTech < 0)
                throw new ArgumentException("Resource values cannot be negative.");

            Water = water;
            Food = food;
            Scrap = scrap;
            Fuel = fuel;
            Energy = energy;
            RareTech = rareTech;
        }

        public void AddWater(int amount)
        {
            ValidateAmount(amount);
            Water += amount;
        }

        public void AddFood(int amount)
        {
            ValidateAmount(amount);
            Food += amount;
        }

        public void AddScrap(int amount)
        {
            ValidateAmount(amount);
            Scrap += amount;
        }

        public void AddFuel(int amount)
        {
            ValidateAmount(amount);
            Fuel += amount;
        }

        public void AddEnergy(int amount)
        {
            ValidateAmount(amount);
            Energy += amount;
        }

        public void AddRareTech(int amount)
        {
            ValidateAmount(amount);
            RareTech += amount;
        }

        public bool HasEnough(
            int water = 0,
            int food = 0,
            int scrap = 0,
            int fuel = 0,
            int energy = 0,
            int rareTech = 0)
        {
            return Water >= water
                && Food >= food
                && Scrap >= scrap
                && Fuel >= fuel
                && Energy >= energy
                && RareTech >= rareTech;
        }

        public bool HasEnough(UnitCost cost)
        {
            if (cost == null)
                return true;

            return HasEnough(
                cost.Water,
                cost.Food,
                cost.Scrap,
                cost.Fuel,
                cost.Energy,
                cost.RareTech);
        }

        public void Spend(
            int water = 0,
            int food = 0,
            int scrap = 0,
            int fuel = 0,
            int energy = 0,
            int rareTech = 0)
        {
            if (!HasEnough(water, food, scrap, fuel, energy, rareTech))
                throw new InvalidOperationException("Not enough resources.");

            Water -= water;
            Food -= food;
            Scrap -= scrap;
            Fuel -= fuel;
            Energy -= energy;
            RareTech -= rareTech;
        }

        public void Spend(UnitCost cost)
        {
            if (cost == null)
                return;

            Spend(
                cost.Water,
                cost.Food,
                cost.Scrap,
                cost.Fuel,
                cost.Energy,
                cost.RareTech);
        }

        public bool TrySpend(UnitCost cost)
        {
            if (!HasEnough(cost))
                return false;

            Spend(cost);
            return true;
        }

        public void Add(
            int water = 0,
            int food = 0,
            int scrap = 0,
            int fuel = 0,
            int energy = 0,
            int rareTech = 0)
        {
            ValidateAmount(water);
            ValidateAmount(food);
            ValidateAmount(scrap);
            ValidateAmount(fuel);
            ValidateAmount(energy);
            ValidateAmount(rareTech);

            Water += water;
            Food += food;
            Scrap += scrap;
            Fuel += fuel;
            Energy += energy;
            RareTech += rareTech;
        }

        private static void ValidateAmount(int amount)
        {
            if (amount < 0)
                throw new ArgumentException("Amount cannot be negative.");
        }

        public void CapToStorage(
        int waterCap,
        int foodCap,
        int scrapCap,
        int fuelCap,
        int energyCap,
        int rareTechCap)
        {
            Water = Math.Min(Water, waterCap);
            Food = Math.Min(Food, foodCap);
            Scrap = Math.Min(Scrap, scrapCap);
            Fuel = Math.Min(Fuel, fuelCap);
            Energy = Math.Min(Energy, energyCap);
            RareTech = Math.Min(RareTech, rareTechCap);
        }
    }
}