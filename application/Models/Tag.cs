using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace application.Models
{
	public class Tag
	{
		[Key]
		public Guid Id { get; set; }

		[Required]
		public String Name { get; set; }

		[Required]
		public Guid TagCategoryId { get; set; }
		public TagCategory TagCategory { get; set; }

		public List<ProductTag> Products { get; set; } = new List<ProductTag>();
	}
}
