using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OkiJobAPI.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OkiJobAPI
{
	public class Program
	{
		public static void Main(string[] args)
		{
			IHost host = CreateHostBuilder(args).Build();
			CreateDbIfNotExists(host);

			host.Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseStartup<Startup>();
				});

		public static void CreateDbIfNotExists(IHost host)
		{
			using IServiceScope scope = host.Services.CreateScope();
			IServiceProvider serviceProvider = scope.ServiceProvider;

			try
			{
				DbInitializer.Initialize(serviceProvider.GetRequiredService<SharedContext>());
			}
			catch (Exception ex)
			{
				serviceProvider.GetRequiredService<ILogger<Program>>().LogError(ex, "An error occured creating the DB");
			}
		}
	}
}
