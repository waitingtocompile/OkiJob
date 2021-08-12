using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OkiJobAPI.Models
{
	public class Material
	{
		public int ID { get; set; }
		[StringLength(50)]
		public string Name { get; set; } = null!;
		public int Price { get; set; }

		public ICollection<MaterialCost> MaterialCosts { get; set; } = null!;

	}
}
