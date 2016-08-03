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
    }

    public static class ProductSummaryExtensions
    {
        private static XNamespace ns = "http://webservices.amazon.com/AWSECommerceService/2011-08-01";
        public static ProductSummary ToProductSummary(this string xmlString)
        {
            var item = XElement.Parse(xmlString)
                                .Elements(ns + "Items")
                                .Single()
                                .Elements(ns + "Item")
                                .Single();

            var lowestNewPrice = Double.Parse(item
                                        .Elements(ns + "OfferSummary")
                                        .Single()
                                        .Elements(ns + "LowestNewPrice")
                                        .Single()
                                        .Elements(ns + "Amount")
                                        .Single()
                                        .Value) / 100;

            var qty = Int32.Parse(item
                            .Elements(ns + "OfferSummary")
                            .Single()
                            .Elements(ns + "TotalNew")
                            .Single()
                            .Value);

            return new ProductSummary()
            {
                //Asin = "", //not needed
                Price = Convert.ToInt64(Math.Ceiling(lowestNewPrice)),
                Qty = qty
            };
        }

        public static bool IsUpdateRequired(this ProductSummary sumProduct, Product dbProd)
        {
            //availability ==> to do
            return !sumProduct.Price.Equals(dbProd.Price);
        }
    }
}
