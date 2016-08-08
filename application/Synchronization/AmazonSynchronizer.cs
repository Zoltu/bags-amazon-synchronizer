using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using application.Data;
using application.Logger;
using application.Models;
using Microsoft.EntityFrameworkCore;

namespace application.Synchronization
{
    public class AmazonSynchronizer : SynchronizerBase
    {
        public AmazonSynchronizer(Configuration config):base(config) {}
        
        protected override void ExecuteUpdateInternal()
        {
            _logger.WriteEntry("##########################################################", LoggingLevel.Info);
            _logger.WriteEntry($"########## Update #{_updatesCount} Started ##########", LoggingLevel.Info);
                
            //_logger.WriteEntry("Fetching products from the database ...", LoggingLevel.Debug);

            var summary = new UpdateSummary()
            {
                //ProductCount = products.Count,
                StartDate = DateTime.Now
            };

            var startIndex = 0;
            
            while (true)
            {
                using (var dbContext = new BagsContext(_config))
                {
                    //Getting products from DB
                    var products = FetchProductsFromDb(dbContext, startIndex, _productsPerBatch);
                    if (products == null || products.Count == 0)
                        break;

                    summary.ProductCount += products.Count;
                    startIndex += products.Count;

                    //_logger.WriteEntry($"{products.Count} products found...", LoggingLevel.Debug);

                    foreach (var dbProd in products)
                    {
                        try
                        {
                            //if cancelled ==> exit
                            if (_cancelToken.IsCancellationRequested)
                                return;

                            ExecuteAndWait(() =>
                                {
                                    var amzProd = _amazonClient.GetProductSummary(dbProd.Asin).Result;
                                    if (!amzProd.IsAvailable)//product unavailable or an error occured while getting product from API
                                    {
                                        summary.UnavailableCount++;
                                    }

                                    if (amzProd.IsUpdateRequired(dbProd.AmazonProduct))
                                    {
                                        summary.UpdatedCount++;
                                    }

                                    UpdateAmazonProduct(dbContext, amzProd, dbProd);

                                    _logger.WriteEntry($"@Update#{_updatesCount} | @{dbProd.Asin} | Current Price : {dbProd.Price} |=> Amazon State : Price : {amzProd.Price} / Available : {amzProd.IsAvailable}", LoggingLevel.Debug);

                                });

                    }
                        catch (Exception ex)
                        {
                            _logger.WriteEntry($"@ExecuteUpdateInternal | @{dbProd.Asin} : {ex.GetLastErrorMessage()}", LoggingLevel.Error);
                            summary.ErrorAsins.Add(dbProd.Asin);
                        }
                    } //foreach
                }//using
                
        }//while

            summary.EndDate = DateTime.Now;
            _logger.WriteEntry($"########## Update #{_updatesCount++} Complete ... ##########", LoggingLevel.Info);
            _logger.WriteEntry($"       {summary.ToString()}", LoggingLevel.Info);
        }
        
        /// <summary>
        /// Executes an action and waits for a certain time  
        /// </summary>
        /// <param name="action">Action to be executed</param>
        /// <param name="delayInMs">Time to wait before returning after execution</param>
        private void ExecuteAndWait(Action action, int delayInMs = 1000)
        {
            _watch.Restart();

            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                _logger.WriteEntry($"@ExecuteAndWait : {ex.GetLastErrorMessage()}", LoggingLevel.Error);
            }

            //_logger.WriteEntry($"Elapsed : {_watch.ElapsedMilliseconds} Ms", LoggingLevel.Debug);

            if (_watch.ElapsedMilliseconds >= delayInMs) //took more then one sec
                return;//don't wait
            else//wait the difference
            {
                Thread.Sleep(delayInMs - Convert.ToInt32(_watch.ElapsedMilliseconds));
            }
        }

        private AmazonProduct GetAmazonProductByAsin(BagsContext dbContext, string asin)
        {
            try
            {
                return dbContext.AmazonProducts
                                //.Include(pr=>pr.Product)
                                .FirstOrDefault(p => p.Asin.Equals(asin, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.WriteEntry($"@GetAmazonProductByAsin | @{asin} : {ex.GetLastErrorMessage()}", LoggingLevel.Error);
            }

            return null;
        }

        private void UpdateAmazonProduct(BagsContext dbContext, ProductSummary prodSum, Product dbProd)
        {
            #region # How it works # 
            /*                
                 +The update is made on the two tables : Product and AmazonProduct
                 +The reason for that is because the Price exists in both tables and I don't know which one you are using in your backend API
                 +When I update the price I do it for Product and AmazonProduct tables
                 +When I insert an AmazonProduct for the first time : I add it in AmazonProduct table
                 +If I update an existing AmazonProduct : I update its properties (price, availability, last checked, ...) and I update the price in the corresponding Product 
            */
            #endregion

            //using (var dbContext = new BagsContext(_config))
            //{
                var pr = GetAmazonProductByAsin(dbContext, prodSum.Asin);
                var price = (prodSum.Price > 0) ? Convert.ToInt32(prodSum.Price) : Convert.ToInt32(dbProd.Price);

                if (pr == null)//it means that this is a new product and must be inserted into the AmazonProduct table
                {
                    pr = new AmazonProduct
                    {
                        Asin = prodSum.Asin,
                        Price = price,
                        LastChecked = DateTime.Now,
                        Available = prodSum.IsAvailable,
                        Product = dbProd
                    };

                    dbContext.AmazonProducts.Add(pr);
                }
                else//update existing amazon product
                {
                    pr.Price = price;
                    pr.Available = prodSum.IsAvailable;
                    pr.LastChecked = DateTime.Now;
                }

            //Update the price in Product.Price
            dbProd.Price = price;

            //if async, the db context can request a new batch before the save is made ==> throws an exception, because it's not thread safe
            dbContext.SaveChanges(); 
            //}

        }

        private List<Product> FetchProductsFromDb(BagsContext dbContext, int startIndex = -1, int productCount = -1)
        {
            try
            {
                if (startIndex < 1)
                    startIndex = 1;

                if (productCount > 0)
                    return dbContext.Products
                                    .Skip(startIndex - 1)
                                    .Take(productCount)
                                    .ToList();
                else
                    return dbContext.Products
                                    .Skip(startIndex - 1)
                                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.WriteEntry($"Error getting products from DB : {ex.Message}", LoggingLevel.Error);
                return null;
            }
        }
    }
}
