using System;
using System.ComponentModel.DataAnnotations;

namespace application.Models
{
    public class AmazonProduct
    {
        [Key]
        public int Id { get; set; }
        
        public int Quantity { get; set; }

        public DateTime LastChecked { get; set; }

        public Product Product { get; set; }    
    }
}
