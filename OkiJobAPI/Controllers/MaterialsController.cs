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
	public class GetMaterialDTO
	{
		public int ID { get; set; }
		[StringLength(50)]
		public string? Name { get; set; }
		public int Price { get; set; }

		public static GetMaterialDTO FromMaterial(Material material)
		{
			return new GetMaterialDTO() {ID = material.ID, Name = material.Name, Price = material.Price };
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
		public async Task<ActionResult<IEnumerable<GetMaterialDTO>>> GetMaterials()
		{
			return await _context.Materials.Select(m => GetMaterialDTO.FromMaterial(m)).ToListAsync();
		}

		/// <summary>
		/// Get detailed information on a material, including it's price and all recorded ships that use it
		/// </summary>
		[HttpGet("{id}")]
		public async Task<ActionResult<Material>> Get(int id)
		{
			return await _context.Materials.Where(m => m.ID == id).SingleAsync();
		}

		/// <summary>
		/// Update the price on a material
		/// </summary>
		/// <param name="id">Material ID</param>
		/// <param name="value">New Price</param>
		/// <returns></returns>
		[HttpPut("{id}")]
		public async Task<IActionResult> Put(int id, [FromBody] int value)
		{
			Material? material = await _context.Materials.FindAsync(id);
			if(material is null)
			{
				return NotFound();
			}

			material.Price = value;
			_context.SaveChanges();
			return Ok();
		}
	}
}
