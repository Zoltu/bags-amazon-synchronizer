using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace application.Models
{
    public class Offer
    {
        public Int32 Price { get; set; }
        public bool IsEligibleForPrime { get; set; }
        public bool HasSellers { get; set; }
    }
}
