using application.Models;
using Microsoft.EntityFrameworkCore;

namespace application.Data
{
	public class BagsContext : DbContext
    {
		public DbSet<Product> Products { get; set; }
		public DbSet<Tag> Tags { get; set; }
		public DbSet<TagCategory> TagCategories { get; set; }
		public DbSet<ProductTag> ProductTags { get; set; }

		private Configuration _configuration;

		public BagsContext(Configuration configuration)
		{
			_configuration = configuration;
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlServer(_configuration.SqlServerConnectionString);
		}
	}
}
