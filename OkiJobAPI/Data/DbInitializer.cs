using OkiJobAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OkiJobAPI.Data
{
	public static class DbInitializer
	{
		public static void Initialize(SharedContext context)
		{
			context.Database.EnsureCreated();

			if(context.Materials.Any())
			{
				return;
			}

			Material Valkite = new Material() { Name = "Valkite", Price = 10 };
			context.Materials.Add(Valkite);
			Material Ajatite = new Material() { Name = "Ajatite", Price = 20 };
			context.Materials.Add(Ajatite);
			Material Bastium = new Material() { Name = "Bastium", Price = 200 };
			context.Materials.Add(Bastium);
			context.SaveChanges();

			Ship TestShip = new Ship() { Name = "TestShip", Designer = "Okim", Description = "A fake ship that doesn't exist" };
			context.Ships.Add(TestShip);
			context.SaveChanges();

			context.MaterialCosts.Add(new MaterialCost() { MaterialID = 1, Amount = 10, ShipID = 1 });
			context.MaterialCosts.Add(new MaterialCost() { MaterialID = 2, Amount = 20, ShipID = 1 });
			context.MaterialCosts.Add(new MaterialCost() { MaterialID = 3, Amount = 30, ShipID = 1 });
			context.SaveChanges();
		}
	}
}
