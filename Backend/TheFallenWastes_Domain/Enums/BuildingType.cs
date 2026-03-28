using System;

namespace TheFallenWastes_Domain.Enums
{
    public enum BuildingType
    {
        // CENTER BUILDINGS
        HeadQuarter,
        Shelter,
        CouncilHall,

        // RESOURCE BUILDINGS
        FarmDome,
        FuelRefinery,
        ScrapForge,
        WaterPurifier,
        SolarArray,

        // STOCK BUILDINGS
        FoodSilo,
        FuelDepot,
        PowerBank,
        ScrapVault,
        WaterTank,

        // SPECIAL STOCK BUILDING
        TechVault,
        RaidVault,

        // MILITARY BUILDINGS
        Barracks,
        Garage,
        Workshop,
        CommandCenter,

        // DEFENSE BUILDINGS
        PerimeterWall,
        WatchTower,

        // RESEARCH BUILDINGS
        TechLab,
        TechSalvager
    }
}