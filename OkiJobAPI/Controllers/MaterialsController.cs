using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OkiJobAPI.Data;
using OkiJobAPI.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OkiJobAPI.Controllers
{
	public class MaterialReducedDTO
	{
		public int ID { get; set; }
		[StringLength(50)]
		public string Name { get; set; } = null!;
		public int Price { get; set; }

		public static MaterialReducedDTO FromMaterial(Material material)
		{
			return new MaterialReducedDTO() {ID = material.ID, Name = material.Name, Price = material.Price };
		}
	}

	public class MaterialWithCostsDTO
	{
		public class MaterialMaterialCostDTO
		{
			public int Amount { get; set; }
			public int ShipID { get; set; }
			public string ShipName { get; set; } = null!;
		}

		public int ID { get; set; }
		[StringLength(50)]
		public string Name { get; set; } = null!;
		public int Price { get; set; }

		public ICollection<MaterialMaterialCostDTO> ShipCosts {get; set;} = null!;


		public static async Task<MaterialWithCostsDTO> FromMaterial(Material material, SharedContext context)
		{
			await context.Entry(material).Collection(m => m.MaterialCosts).LoadAsync();
			return new MaterialWithCostsDTO() { ID = material.ID, Name = material.Name, Price = material.Price, ShipCosts =  (await Task.WhenAll(material.MaterialCosts!.Select(c => FromCost(c, context)))).ToList() };
		}

		public static async Task<MaterialMaterialCostDTO> FromCost(MaterialCost cost, SharedContext context)
		{
			await context.Entry(cost).Reference(c => c.Ship).LoadAsync();
			return new MaterialMaterialCostDTO() { Amount = cost.Amount, ShipID = cost.ShipID, ShipName = cost.Ship!.Name };
		}
	}

	[Route("Materials")]
	[ApiController]
	public class MaterialsController : ControllerBase
	{
		private readonly SharedContext _context;

		public MaterialsController(SharedContext context)
		{
			_context = context;
		}


		

		/// <summary>
		/// Get a list of all materials and their prices
		/// </summary>
		[HttpGet]
		public async Task<ActionResult<IEnumerable<MaterialReducedDTO>>> GetMaterials()
		{
			return await _context.Materials.Select(m => MaterialReducedDTO.FromMaterial(m)).ToListAsync();
		}

		/// <summary>
		/// Get detailed information on a material, including it's price and all recorded ships that use it
		/// </summary>
		[HttpGet("{id}")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult<MaterialWithCostsDTO>> GetMaterial(int id)
		{
			Material? material = await _context.Materials.FindAsync(id);
			if(material is null)
			{
				return NotFound();
			}
			
			return await MaterialWithCostsDTO.FromMaterial(material, _context);
		}

		/// <summary>
		/// Update the price on a material
		/// </summary>
		/// <param name="id" example="1">Material ID</param>
		/// <param name="value" example="150">New Price</param>
		/// <response code  ="204">Added</response>
		/// <response code="400">Invalid value format</response>
		/// <reponse code="404">Material does not exist</reponse>
		/// <returns></returns>
		[HttpPatch("{id}")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> PatchMaterial(int id, [FromBody] int value)
		{
			Material? material = await _context.Materials.FindAsync(id);
			if(material is null)
			{
				return NotFound();
			}

			material.Price = value;
			await _context.SaveChangesAsync();
			return NoContent();
		}
	}
}
