using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace application.Models
{
    //N.B : if you inherit from Product ==> unwanted EF effect 
    public class AmazonProduct
    {
        [Key]
        public Int32 Id { get; set; }

        //[Required]
        public Int32 Quantity { get; set; }
        public DateTime LastChecked { get; set; }
        
    }
}
