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
            XElement item;
            double lowestNewPrice;
            int qty;

            try
            {
                item = XElement.Parse(xmlString)
                            .Elements(ns + "Items")
                            .Single()
                            .Elements(ns + "Item")
                            .Single();

                 lowestNewPrice = Double.Parse(item
                                            .Elements(ns + "OfferSummary")
                                            .Single()
                                            .Elements(ns + "LowestNewPrice")
                                            .Single()
                                            .Elements(ns + "Amount")
                                            .Single()
                                            .Value) / 100;

                 qty = Int32.Parse(item
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
            catch (Exception ex)
            {
                //some products become out of stock or removed for good so 
                  //< OfferSummary >
                  //  < TotalNew > 0 </ TotalNew >
                  //  < TotalUsed > 0 </ TotalUsed >
                  //  < TotalCollectible > 0 </ TotalCollectible >
                  //  < TotalRefurbished > 0 </ TotalRefurbished >
                  //</ OfferSummary >
                  //< Offers >
                  //  < TotalOffers > 0 </ TotalOffers >
                  //  < TotalOfferPages > 0 </ TotalOfferPages >
                  //  < MoreOffersUrl > 0 </ MoreOffersUrl >
                  //</ Offers >
            }

            return null;
        }

        public static bool IsUpdateRequired(this ProductSummary sumProduct, Product dbProd)
        {
            //availability ==> to do
            return !sumProduct.Price.Equals(dbProd.Price);
        }
    }
}
