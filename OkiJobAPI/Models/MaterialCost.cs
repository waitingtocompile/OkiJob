using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OkiJobAPI.Models
{
	public class MaterialCost
	{
		public int ShipID { get; set; }
		public Ship Ship { get; set; } = null!;
		public int MaterialID { get; set; }
		public Material Material { get; set; } = null!;
		public int Amount { get; set; }
	}
}
