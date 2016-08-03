using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace application.Models
{
	public class TagCategory
    {
		[Key]
		public Guid Id { get; set; }

		[Required]
		public String Name { get; set; }

		public List<Tag> Tags { get; set; } = new List<Tag>();
	}
}
