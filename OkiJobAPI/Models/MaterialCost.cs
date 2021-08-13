using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OkiJobAPI.Models
{
	public class MaterialCost
	{
		public int ShipID { get; set; }
		[Required]
		public Ship? Ship { get; set; }
		public int MaterialID { get; set; }
		[Required]
		public Material? Material { get; set; }
		public int Amount { get; set; }
	}
}
