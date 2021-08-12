﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OkiJobAPI.Data;
using OkiJobAPI.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OkiJobAPI.Controllers
{

	public class ShipReducedDTO
	{
		public int ID { get; set; }
		[StringLength(50)]
		public string Name { get; set; } = null!;
		[StringLength(50)]
		public string Designer { get; set; } = null!;
		public string? Description { get; set; }

		public static ShipReducedDTO FromShip(Ship ship)
		{
			return new ShipReducedDTO() { ID = ship.ID, Name = ship.Name, Designer = ship.Designer, Description = ship.Description };
		}
	}

	public class ShipWithCostsDTO
	{
		public class ShipMaterialCostDTO
		{
			public int Amount { get; set; }
			public int MaterialId { get; set; }
			public string MaterialName { get; set; } = null!;
		}

		public int ID { get; set; }
		[StringLength(50)]
		public string Name { get; set; } = null!;
		[StringLength(50)]
		public string Designer { get; set; } = null!;
		public string? Description { get; set; }
		public ICollection<ShipMaterialCostDTO> MaterialCosts { get; set; } = null!;

		public static async Task<ShipWithCostsDTO> FromShip(Ship ship, SharedContext context)
		{
			await context.Entry(ship).Collection(s => s.MaterialCosts).LoadAsync();
			return new ShipWithCostsDTO() { ID = ship.ID, Name = ship.Name, Designer = ship.Designer, Description = ship.Description, MaterialCosts = (await Task.WhenAll(ship.MaterialCosts.Select(c => FromCost(c, context)))).ToList() };
		}

		public static async Task<ShipMaterialCostDTO> FromCost(MaterialCost cost, SharedContext context)
		{
			await context.Entry(cost).Reference(c => c.Material).LoadAsync();
			return new ShipMaterialCostDTO() { Amount = cost.Amount, MaterialId = cost.MaterialID, MaterialName = cost.Material.Name };
		}

	}

	public class NewMaterialCostDTO
	{
		public int MaterialID { get; set; }
		public int Amount { get; set; }
	}





	[Route("Ships")]
	[ApiController]
	public class ShipController : ControllerBase
	{
		private readonly SharedContext _context;

		public ShipController(SharedContext context)
		{
			_context = context;
		}

		/// <summary>
		/// Get a list of all ships and their basic information
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public async Task<ActionResult<IEnumerable<ShipReducedDTO>>> GetShips()
		{
			return await _context.Ships.Select(s => ShipReducedDTO.FromShip(s)).ToListAsync();
		}

		/// <summary>
		/// Get a detailed breakdown of a specific ship
		/// </summary>
		/// <returns></returns>
		[HttpGet("{id}")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult<ShipWithCostsDTO>> GetShip(int id)
		{
			Ship? ship = await _context.Ships.FindAsync(id);
			if(ship is null)
			{
				return NotFound();
			}

			return await ShipWithCostsDTO.FromShip(ship, _context);
		}

		// TODO: ship general put/post/patch/delete endpoints

		/// <summary>
		/// Update the material costs of a ship.
		/// </summary>
		/// <remarks>
		/// Note that ALL costs need to be included, any costs not featured in the list will be premanently deleted
		/// </remarks>
		/// <response code  ="204">Added</response>
		/// <response code="400">Invalid format, or unrecognized or duplicate material ID</response>
		/// <reponse code="404">Ship does not exist</reponse>
		[HttpPut("Cost/{id}")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> UpdateCosts(int id, ICollection<NewMaterialCostDTO> newCosts)
		{
			Ship? ship = await _context.Ships.FindAsync(id);
			if (ship is null)
				return NotFound();

			if(newCosts.Select(c => c.MaterialID).Distinct().Count() != newCosts.Count)
			{
				return BadRequest("Duplicate material IDs detected");
			}

			var allMaterialIds = _context.Materials.Select(c => c.ID);
			foreach(int idx in newCosts.Select(c => c.MaterialID))
			{
				var b = allMaterialIds.Contains(idx);
				if (!allMaterialIds.Contains(idx))
				{
					return BadRequest($"Unkown Material ID {idx}");
				}
			}

			foreach (MaterialCost oldCost in _context.MaterialCosts.Where(c => c.Ship == ship))
			{
				_context.MaterialCosts.Remove(oldCost);
			}

			await _context.SaveChangesAsync();
			await _context.MaterialCosts.AddRangeAsync(newCosts.Select(c => new MaterialCost() { MaterialID = c.MaterialID, Amount = c.Amount, ShipID = ship.ID }));
			
			await _context.SaveChangesAsync();
			return NoContent();
		}

	}
}