using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace application.Models
{
    public class ProductSummary
    {
        public string Asin { get; set; }
        public Int64 Price { get; set; }
        public int Qty { get; set; }
        public bool IsPrime { get; set; }
        public bool Available { get; set; }

    }
    
}
