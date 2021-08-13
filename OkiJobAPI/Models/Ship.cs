using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OkiJobAPI.Models
{
	public class Ship
	{
		public int ID { get; set; }
		[StringLength(50)]
		public string Name { get; set; } = null!;
		[StringLength(50)]
		public string Designer { get; set; } = null!;
		public string? Description { get; set; }
		public ICollection<MaterialCost>? MaterialCosts { get; set; }
	}
}
