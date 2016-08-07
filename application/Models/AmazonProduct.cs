using System;
using System.ComponentModel.DataAnnotations;

namespace application.Models
{
    public class AmazonProduct
    {
        [Key]
        public Int32 Id { get; set; }

        public Int32 Price { get; set; }

        public Boolean Available { get; set; }

        public String Asin { get; set; }

        public DateTimeOffset LastChecked { get; set; }

        [Required]
        public Product Product { get; set; }
    }
}
