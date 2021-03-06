using Microsoft.AspNetCore.Http;
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

	public class ShipMaterialCostDTO
	{
		public int Amount { get; set; }
		public int MaterialID { get; set; }
		public string? MaterialName { get; set; }

		public static async Task<ShipMaterialCostDTO> FromCost(MaterialCost cost, SharedContext context)
		{
			if(cost.Material is null) await context.Entry(cost).Reference(c => c.Material).LoadAsync();
			return new ShipMaterialCostDTO() { Amount = cost.Amount, MaterialID = cost.MaterialID, MaterialName = cost.Material!.Name };
		}
	}

	public class ShipWithCostsDTO
	{
		public int ID { get; set; }
		[StringLength(50)]
		public string Name { get; set; } = null!;
		[StringLength(50)]
		public string Designer { get; set; } = null!;
		public string? Description { get; set; }
		public ICollection<ShipMaterialCostDTO> MaterialCosts { get; set; } = null!;

		public static async Task<ShipWithCostsDTO> FromShip(Ship ship, SharedContext context)
		{
			if(ship.MaterialCosts is null) await context.Entry(ship).Collection(s => s.MaterialCosts).LoadAsync();
			return new ShipWithCostsDTO() { ID = ship.ID, Name = ship.Name, Designer = ship.Designer, Description = ship.Description, MaterialCosts = (await Task.WhenAll(ship.MaterialCosts!.Select(c => ShipMaterialCostDTO.FromCost(c, context)))).ToList() };
		}
	}
	public class NewMaterialCostDTO
	{
		public int MaterialID { get; set; }
		public int Amount { get; set; }
	}

	public class PatchShipDTO
	{
		[StringLength(50)]
		public string? Name { get; set; } = null!;
		[StringLength(50)]
		public string? Designer { get; set; } = null!;
		public string? Description { get; set; }
		public ICollection<NewMaterialCostDTO>? MaterialCosts { get; set; }

		//note: this does not apply material costs, which must be performed seperately
		public void ApplyToShip(Ship ship)
		{
			if(Name is not null)
			{
				ship.Name = Name;
			}

			if(Designer is not null)
			{
				ship.Designer = Designer;
			}

			if(Description is not null)
			{
				ship.Description = Description == "" ? null : Description;
			}
		}
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
			Ship? ship = await _context.Ships.Include(s => s.MaterialCosts).ThenInclude(s => s.Material).SingleAsync(s => s.ID == id);
			if(ship is null)
			{
				return NotFound();
			}

			return await ShipWithCostsDTO.FromShip(ship, _context);
		}

		/// <summary>
		/// Create a new ship with given costs.
		/// </summary>
		/// <param name="newShipDTO">The new ship to create. Note that the "id" field in the ship object will be ignored, as will all "materialName" properties </param>
		/// <returns></returns>
		[HttpPost]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<ShipWithCostsDTO>> PostShip(ShipWithCostsDTO newShipDTO)
		{
			Ship ship = new Ship() { Name = newShipDTO.Name, Designer = newShipDTO.Designer, Description = newShipDTO.Description };
			
			if (CheckMaterialsExistAndNoDuplicates(newShipDTO.MaterialCosts.Select(c => c.MaterialID)) is BadRequestObjectResult res)
			{
				return res;
			}

			_context.Ships.Add(ship);
			await _context.SaveChangesAsync();

			IEnumerable<MaterialCost> materialCosts = newShipDTO.MaterialCosts.Select(c => new MaterialCost() { MaterialID = c.MaterialID, ShipID = ship.ID, Amount = c.Amount });
			_context.MaterialCosts.AddRange(materialCosts);
			await _context.SaveChangesAsync();

			return CreatedAtAction("GetShip", new { id = ship.ID }, await ShipWithCostsDTO.FromShip(ship, _context));

		}

		/// <summary>
		/// Update an existing ship with new information
		/// </summary>
		/// <param name="id">The ID of the ship to updated</param>
		/// <param name="updateShipDTO">The properties to alter. Properties that should be left unchanged can be omitted. To delte a description, make sure to submit and empty string</param>
		/// <returns></returns>
		/// <response code  ="204">Added</response>
		/// <response code="400">Invalid format, or unrecognized or duplicate material ID</response>
		/// <reponse code="404">Ship does not exist</reponse>
		[HttpPatch("{id}")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult<ShipWithCostsDTO>> PatchShip(int id, PatchShipDTO updateShipDTO)
		{
			Ship? ship = await _context.Ships.FindAsync(id);
			if (ship is null)
			{
				return NotFound();
			}

			if (updateShipDTO.MaterialCosts is not null && CheckMaterialsExistAndNoDuplicates(updateShipDTO!.MaterialCosts.Select(c => c.MaterialID)) is BadRequestObjectResult res)
			{
				return res;
			}

			updateShipDTO.ApplyToShip(ship);

			if(updateShipDTO.MaterialCosts is not null)
			{
				_context.MaterialCosts.RemoveRange(_context.MaterialCosts.Where(c => c.ShipID == id));
				IEnumerable<MaterialCost> materialCosts = updateShipDTO.MaterialCosts.Select(c => new MaterialCost()
				{ 
					MaterialID = c.MaterialID, ShipID = ship.ID, Amount = c.Amount
				});
				_context.MaterialCosts.AddRange(materialCosts);
			}
			
			await _context.SaveChangesAsync();

			return CreatedAtAction("GetShip", new { id = ship.ID }, await ShipWithCostsDTO.FromShip(ship, _context));
		}

		/// <summary>
		/// Delete a ship by ID
		/// </summary>
		/// <param name="id">ID of the ship to be deleted</param>
		/// <returns></returns>
		/// <response code  ="200">Ship deleted</response>
		/// <reponse code="404">Ship does not exist</reponse>
		[HttpDelete("{id}")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> DelteShip(int id)
		{
			Ship? ship = await _context.Ships.FindAsync(id);
			if (ship is null)
			{
				return NotFound();
			}

			_context.Ships.Remove(ship);
			await _context.SaveChangesAsync();
			return Ok();
		}

		/// <summary>
		/// Update the material costs of a ship.
		/// </summary>
		/// <remarks>
		/// Note that ALL costs need to be included, any costs not featured in the list will be permanently deleted
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

			if(CheckMaterialsExistAndNoDuplicates(newCosts.Select(c => c.MaterialID)) is BadRequestObjectResult res)
			{
				return res;
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

		private BadRequestObjectResult? CheckMaterialsExistAndNoDuplicates(IEnumerable<int> ids)
		{
			if(ids.Distinct().Count() != ids.Count())
			{
				return BadRequest("Duplicate material IDs detected");
			}

			var allMaterialIds = _context.Materials.Select(c => c.ID);
			foreach (int id in ids)
			{
				if (!allMaterialIds.Contains(id))
				{
					return BadRequest($"Unkown Material ID {id}");
				}
			}

			return null;
		}

	}
}
