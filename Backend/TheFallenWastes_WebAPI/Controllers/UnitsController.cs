using System.Linq;
using Microsoft.AspNetCore.Mvc;
using TheFallenWastes_Domain.UnitFactory;

namespace TheFallenWastes_WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UnitsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetAllUnits()
        {
            var units = UnitFactory.CreateStarterUnits();

            var result = units.Select(u => new
            {
                name = u.Name,
                description = u.Description,
                role = u.Role,
                unitType = u.UnitType.ToString(),
                facility = u.Facility.ToString(),
                attackType = u.AttackType.ToString(),
                iconKey = u.IconKey,

                attackPower = u.AttackPower,
                defenseVsBallistic = u.DefenseVsBallistic,
                defenseVsImpact = u.DefenseVsImpact,
                defenseVsEnergy = u.DefenseVsEnergy,

                speed = u.Speed,
                capacityCost = u.CapacityCost,
                carryCapacity = u.CarryCapacity,
                upkeep = u.Upkeep,
                buildTimeSeconds = u.BuildTimeSeconds,

                cost = new
                {
                    water = u.Cost.Water,
                    food = u.Cost.Food,
                    scrap = u.Cost.Scrap,
                    fuel = u.Cost.Fuel,
                    energy = u.Cost.Energy,
                    rareTech = u.Cost.RareTech
                }
            });

            return Ok(result);
        }
    }
}