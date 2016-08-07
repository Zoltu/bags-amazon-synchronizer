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
        public bool IsAvailable { get; set; }

    }

    public static class ProductSummaryExtensions
    {
        private static XNamespace ns = "http://webservices.amazon.com/AWSECommerceService/2011-08-01";
        public static ProductSummary ToProductSummary(this string xmlString, string asin)
        {
            XElement item;
            XElement offers;
            double lowestNewPrice = -1;
            //int qty;

            try
            {
                //If there is prime then set the price to the lowest prime offer and mark the product as available.
                //For now, if there is no Amazon Prime offer then mark the item as unavailable and set the price to the lowest ​new​ offer.
                //if a product has no offer it means not available because it has no seller
                //Mark those as Unavailable and leave the price at whatever it was before.

                item = XElement.Parse(xmlString)
                                .Single("Items")
                                .Single("Item");

                //qty = Int32.Parse(item
                //           .Single("OfferSummary")
                //           .Single("TotalNew")
                //           .Value);

                lowestNewPrice = Double.Parse(item
                                        .Single("OfferSummary")
                                        .Single("LowestNewPrice")
                                        .Single("Amount")
                                        .Value) / 100;

                //get the offers array
                offers = item.Single("Offers");

                //if it has at least an offer (prime or not isn't decided yet)
                if (Convert.ToInt32(offers.Single("TotalOffers").Value) > 0)
                {
                    var bestOffer = GetBestPrimeOffer(offers);//i.e : prime + lowest price
                    if (bestOffer == null) //i.e : there was no prime offer
                    {
                        return new ProductSummary()
                        {
                            Asin = asin,
                            Price = Convert.ToInt32(lowestNewPrice),
                            IsPrime = false,
                            IsAvailable = false
                        };
                    }

                    //if there was a prime offer
                    return new ProductSummary()
                    {
                        Asin = asin,
                        Price = bestOffer.Price,
                        IsPrime = bestOffer.IsEligibleForPrime,//always true
                        IsAvailable = true
                    };
                }

                //if TotalOffers = 0 ==> Unavailable
                
            }
            catch (Exception ex)
            {
                //some products become out of stock or removed for good
            }

            //return null;
            return new ProductSummary()
            {
                Asin = asin, //not needed
                Price = Convert.ToInt64(Math.Ceiling(lowestNewPrice))
            };
        }

        public static bool IsUpdateRequired(this ProductSummary sumProduct, AmazonProduct dbProd)
        {
            if (dbProd == null)//this happens if the product is new and hasn't been added to the amazon table
                return true;

            return !sumProduct.Price.Equals(dbProd.Price) || 
                   !sumProduct.IsAvailable.Equals(dbProd.Available);
        }

        /// <summary>
        /// Get the offer with the lowest price
        /// </summary>
        /// <param name="offers"></param>
        /// <returns></returns>
        private static Offer GetBestPrimeOffer(XElement offers)
        {
            var offerList = offers.Elements(ns + "Offer")
                                  .Select(offer => new Offer
                                    {
                                        Price = Int32.Parse(offer.Single("OfferListing").Single("Price").Single("Amount").Value) / 100 ,
                                        IsEligibleForPrime = Convert.ToBoolean(Convert.ToInt32(offer.Single("OfferListing").Single("IsEligibleForPrime").Value)) //if Convert.ToInt32 is omitted ==> error converting to bool
                                  });
           
            return offerList.OrderBy(offer=>offer.Price)//order is ascending so first is lowest
                            .FirstOrDefault(o=>o.IsEligibleForPrime);
        }

        /// <summary>
        /// Get a single element with the specified name
        /// </summary>
        /// <param name="input"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static XElement Single(this XElement input, string name)
        {
            return input.Elements(ns + name).Single();
        }
    }
}
