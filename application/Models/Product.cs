using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace application.Models
{
	public class Product
	{
		[Key]
		public Int32 Id { get; set; }

		[Required]
		public String Name { get; set; }
		[Required]
		public Int64 Price { get; set; }
		[Required, MaxLength(450)]
		public String Asin { get; set; }

		// JSON serialized IEnumerable<Image>
		[Required]
		public String ImagesJson { get; set; }
		public List<ProductTag> Tags { get; set; } = new List<ProductTag>();

		public class Image
		{
			[JsonProperty(PropertyName = "priority")]
			public UInt32 Priority { get; set; }
			[JsonProperty(PropertyName = "small")]
			public String Small { get; set; }
			[JsonProperty(PropertyName = "medium")]
			public String Medium { get; set; }
			[JsonProperty(PropertyName = "large")]
			public String Large { get; set; }
		}
	}
}
