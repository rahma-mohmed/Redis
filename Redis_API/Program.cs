
namespace Redis_API
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			builder.Services.AddControllers();
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();

			#region Memory Cache

			// Add memory cache services
			builder.Services.AddMemoryCache();

			#region redis distributed cache
			builder.Services.AddStackExchangeRedisCache(options =>
			{
				options.Configuration = "localhost:6379";
				options.InstanceName = "SampleInstance";
			});

			// options.Configuration = "localhost:6379" is the port of redis server

			builder.Services.AddSession();
			#endregion

			#endregion

			var app = builder.Build();

			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseHttpsRedirection();

			app.UseAuthorization();

			app.UseSession();

			app.MapControllers();

			app.Run();
		}
	}
}
