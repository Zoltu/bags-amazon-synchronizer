﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using application.Models;

namespace application
{
    public static class Extensions
    {
        public static string GetLastErrorMessage(this Exception exception)
        {
            if (exception == null) return "";

            string message = exception.Message;
            Exception inner = exception.InnerException;
            while (!string.IsNullOrEmpty(inner?.Message))
            {
                message = inner.Message;
                inner = inner.InnerException;
            }

            return message;
        }
    }

    public static class ProductSummaryExtensions
    {
        private static XNamespace ns = "http://webservices.amazon.com/AWSECommerceService/2011-08-01";
        private static ProductSummary ToProductSummary(this XElement item)
        {
            XElement offers;
            string asin = string.Empty;
            double lowestNewPrice = -1;

            try
            {
                //If there is prime then set the price to the lowest prime offer and mark the product as available.
                //For now, if there is no Amazon Prime offer then mark the item as unavailable and set the price to the lowest ​new​ offer.
                //if a product has no offer it means not available because it has no seller
                //Mark those as Unavailable and leave the price at whatever it was before.

                asin = item.Single("ASIN").Value;

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
                    var bestOffer = GetBestPrimeOffer(offers);

                    //if there was a prime offer
                    if (bestOffer != null)
                        return new ProductSummary()
                        {
                            Asin = asin,
                            Price = RoundPrice(bestOffer.Price),
                            IsPrime = bestOffer.IsEligibleForPrime,//always true
                            Available = true
                        };

                    else//if there were no prime offers, just get the lowest non prime price and set availability to true 
                        return new ProductSummary()
                        {
                            Asin = asin,
                            Price = RoundPrice(lowestNewPrice),
                            Available = true,
                            IsPrime = false
                        };
                }
                
            }
            catch (Exception ex)
            {
                //some products become out of stock or removed for good
            }

            //if the code reaches here that means that there was an exception during execution ==> There was no offer for the product, prime or not prime
            //so just set it to not available
            return new ProductSummary()
            {
                Asin = asin,
                Price = RoundPrice(lowestNewPrice),
                Available = false,
                IsPrime = false
            };

        }
        public static List<ProductSummary> ToProductSummaryList(this string xmlString)
        {
            return XElement.Parse(xmlString)
                            .Single("Items")
                            .Elements(ns + "Item")
                            .Select(item => item.ToProductSummary())
                            .ToList();
        }

        /// <summary>
        /// Checks if a product needs to be updated
        /// </summary>
        /// <param name="sumProduct">product from Amazon API</param>
        /// <param name="dbProd">product from the database</param>
        /// <returns></returns>
        public static bool IsUpdateRequired(this ProductSummary sumProduct, AmazonProduct dbProd)
        {
            //this happens if the product is new and hasn't been added to the amazon table
            if (dbProd == null)
                return true;

            return !sumProduct.Price.Equals(dbProd.Price) ||
                   !sumProduct.Available.Equals(dbProd.Available);
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
                                      Price = Int32.Parse(offer.Single("OfferListing").Single("Price").Single("Amount").Value) / 100,
                                      IsEligibleForPrime = Convert.ToBoolean(Convert.ToInt32(offer.Single("OfferListing").Single("IsEligibleForPrime").Value)) //if Convert.ToInt32 is omitted ==> error converting to bool
                                  });

            return offerList.OrderBy(offer => offer.Price)//order is ascending so first is lowest
                            .FirstOrDefault(o => o.IsEligibleForPrime);
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

        /// <summary>
        /// Rounds the price of a product to the nearest $5
        /// </summary>
        /// <param name="price"></param>
        /// <returns></returns>
        private static Int64 RoundPrice(double price)
        {
            var roundedPrice = Math.Ceiling(price);

            while (roundedPrice % 5 != 0)
            {
                roundedPrice++;
            }
            return Convert.ToInt64(roundedPrice);
        }
    }
}
