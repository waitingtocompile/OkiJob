using Microsoft.EntityFrameworkCore;
using OkiJobAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OkiJobAPI.Data
{
	public class SharedContext : DbContext
	{
		public DbSet<Ship> Ships => Set<Ship>();
		public DbSet<Material> Materials => Set<Material>();
		public DbSet<MaterialCost> MaterialCosts => Set<MaterialCost>();


		public SharedContext(DbContextOptions<SharedContext> options) : base(options)
		{ }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<MaterialCost>().HasKey(m => new { m.ShipID, m.MaterialID });
			modelBuilder.Entity<MaterialCost>().HasOne(m => m.Ship).WithMany(s => s.MaterialCosts).HasForeignKey(m => m.ShipID);
			modelBuilder.Entity<MaterialCost>().HasOne(m => m.Material).WithMany(m => m.ShipCosts).HasForeignKey(m => m.MaterialID);

			modelBuilder.Entity<Ship>().ToTable("Ships");
			modelBuilder.Entity<Material>().ToTable("Materials");
			modelBuilder.Entity<MaterialCost>().ToTable("MaterialCosts");

		}
	}
}
