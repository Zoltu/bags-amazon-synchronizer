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
        public bool IsAvailable => Quantity > 0;
        public bool IsMarkedForDeletion { get; set; }//some ASINs are no longer available in Amazon so they should be removed from the db by the admin
        public DateTime LastChecked { get; set; }

        public Product Product { get; set; }    
    }
}
