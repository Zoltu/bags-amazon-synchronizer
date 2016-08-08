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
            _logger.WriteEntry("##########################################################", LoggingLevel.Info, _updatesCount);
            _logger.WriteEntry($"########## Update #{_updatesCount} Started ##########", LoggingLevel.Info, _updatesCount);
                
            //_logger.WriteEntry("Fetching products from the database ...", LoggingLevel.Debug);

            var summary = new UpdateSummary()
            {
                //ProductCount = products.Count,
                StartDate = DateTime.Now
            };

            var startIndex = 0;
            var count = 0;
            while (true)
            {
                using (var dbContext = new BagsContext(_config))
                {
                    //Getting products from DB
                    var products = GetProductsFromDb(dbContext, startIndex, _productsPerBatch);
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
                                var productSummary = _amazonClient.GetProductSummary(dbProd.Asin).Result;
                                productSummary.Price = (productSummary.Price > 0) ? Convert.ToInt32(productSummary.Price) : Convert.ToInt32(dbProd.Price);

                                if (!productSummary.IsAvailable)
                                {
                                    summary.UnavailableCount++;
                                }

                                if (productSummary.IsUpdateRequired(dbProd.AmazonProduct))
                                {
                                    UpdateAmazonProduct(dbContext, productSummary, dbProd);
                                    summary.UpdatedCount++;
                                }
                                
                                _logger.WriteEntry($"@Update#{_updatesCount} | @Product#{count}| @{dbProd.Asin} | Current Price : {dbProd.Price} |=> Amazon State : Price : {productSummary.Price} / Available : {productSummary.IsAvailable}", LoggingLevel.Debug, _updatesCount);

                            });

                        }
                        catch (Exception ex)
                        {
                            _logger.WriteEntry($"@ExecuteUpdateInternal | @{dbProd.Asin} : {ex.GetLastErrorMessage()}", LoggingLevel.Error, _updatesCount);
                            summary.ErrorAsins.Add(dbProd.Asin);
                        }
                        finally
                        {
                            count++;
                        }
                    } //foreach


                    dbContext.SaveChanges();

                }//using
                
            }//while

            summary.EndDate = DateTime.Now;
            _logger.WriteEntry($"########## Update #{_updatesCount++} Complete ... ##########", LoggingLevel.Info, _updatesCount);
            _logger.WriteEntry($"       {summary.ToString()}", LoggingLevel.Info, _updatesCount);
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
                _logger.WriteEntry($"@ExecuteAndWait : {ex.GetLastErrorMessage()}", LoggingLevel.Error, _updatesCount);
            }

            _logger.WriteEntry($"Elapsed : {_watch.ElapsedMilliseconds} Ms", LoggingLevel.Debug, _updatesCount);

            if (_watch.ElapsedMilliseconds >= delayInMs) //took more then one sec
                return;//don't wait
            else//wait the difference
            {
                Thread.Sleep(delayInMs - Convert.ToInt32(_watch.ElapsedMilliseconds));
            }
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

            var pr = GetAmazonProductByAsin(dbContext, prodSum.Asin);

            if (pr == null)//it means that this is a new product and must be inserted into the AmazonProduct table
            {
                pr = new AmazonProduct
                {
                    Asin = prodSum.Asin,
                    Price = Convert.ToInt32(prodSum.Price),
                    LastChecked = DateTime.Now,
                    Available = prodSum.IsAvailable,
                    Product = dbProd
                };

                dbContext.AmazonProducts.Add(pr);
            }
            else//update existing amazon product
            {
                pr.Price = Convert.ToInt32(prodSum.Price);
                pr.Available = prodSum.IsAvailable;
                pr.LastChecked = DateTime.Now;
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
                _logger.WriteEntry($"@GetAmazonProductByAsin | @{asin} : {ex.GetLastErrorMessage()}", LoggingLevel.Error, _updatesCount);
            }

            return null;
        }

        private List<Product> GetProductsFromDb(BagsContext dbContext, int startIndex = -1, int productCount = -1)
        {
            try
            {
                if (startIndex < 1)
                    startIndex = 1;

                if (productCount > 0)
                    return dbContext.Products
                                    .Include(p=>p.AmazonProduct)
                                    .Skip(startIndex - 1)
                                    .Take(productCount)
                                    //.AsNoTracking() ==> when Product price won't need update
                                    .ToList();
                else
                    return dbContext.Products
                                    .Include(p => p.AmazonProduct)
                                    .Skip(startIndex - 1)
                                    //.AsNoTracking()
                                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.WriteEntry($"Error getting products from DB : {ex.Message}", LoggingLevel.Error, _updatesCount);
                return null;
            }
        }
    }
}
