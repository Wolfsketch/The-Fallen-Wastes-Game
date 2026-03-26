using System;

namespace TheFallenWastes_Domain.Entities
{
    public class UnitCost
    {
        public int Water { get; private set; }
        public int Food { get; private set; }
        public int Scrap { get; private set; }
        public int Fuel { get; private set; }
        public int Energy { get; private set; }
        public int RareTech { get; private set; }

        public UnitCost(
            int water = 0,
            int food = 0,
            int scrap = 0,
            int fuel = 0,
            int energy = 0,
            int rareTech = 0)
        {
            if (water < 0 || food < 0 || scrap < 0 || fuel < 0 || energy < 0 || rareTech < 0)
                throw new ArgumentException("Unit cost values cannot be negative.");

            Water = water;
            Food = food;
            Scrap = scrap;
            Fuel = fuel;
            Energy = energy;
            RareTech = rareTech;
        }
    }
}