using System;
using System.Linq;
using System.Threading;
using application.Data;
using application.Log;
using application.Models;
using Microsoft.EntityFrameworkCore;

namespace application.Synchronization
{
    public class AmazonSynchronizer : SynchronizerBase
    {
        public AmazonSynchronizer(Configuration config):base(config) {}
        
        protected override void ExecuteUpdateInternal()
        {
            using (var dbContext = new BagsContext(_config))
            {
                _logger.WriteEntry("##########################################################", LoggingLevel.Info);
                _logger.WriteEntry($"########## Update #{_updatesCount} Started ##########", LoggingLevel.Info);
                
                _logger.WriteEntry("Fetching products from the database ...", LoggingLevel.Debug);
                
                //Getting products from DB
                var products = dbContext.Products.ToList();
                //var amazonProducts = dbContext.AmazonProducts.Include(p => p.Product).ToList();
                if (products == null || products.Count == 0)
                    return;

                var summary = new UpdateSummary()
                {
                    ProductCount = products.Count,
                    StartDate = DateTime.Now
                };

                _logger.WriteEntry($"{products.Count} products found...", LoggingLevel.Debug);

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
                            if (amzProd == null)//usually the product wasn't found or it's not available qty wise ==> either way set qty to 0 to mark its unavailability
                            {
                                summary.ErrorAsins.Add(dbProd.Asin);
                                UpdateAmazonProduct(dbContext, dbProd, 0);
                                dbContext.SaveChanges();
                                return;
                            }
                                
                            _logger.WriteEntry($"@Update#{_updatesCount} | @{dbProd.Asin} | Current Price : {dbProd.Price} |==> Amazon State : Price : {amzProd.Price} / Qty : {amzProd.Qty}", LoggingLevel.Debug);

                            if (amzProd.IsUpdateRequired(dbProd))
                            {
                                //price update will be made in Product, Qty,... in AmazonProduct
                                dbProd.Price = amzProd.Price;
                                UpdateAmazonProduct(dbContext, dbProd, amzProd.Qty);
                                dbContext.SaveChanges();//if async, the db context can request a new batch before the save is made ==> throws an exception, because it's not thread safe
                                summary.UpdatedCount++;
                            }
                                
                        });

                    }
                    catch (Exception ex)
                    {
                        _logger.WriteEntry($"@{dbProd.Asin} : {ex.GetLastErrorMessage()}", LoggingLevel.Error);
                        summary.ErrorAsins.Add(dbProd.Asin);
                    }
                }
                
                summary.EndDate = DateTime.Now;
                _logger.WriteEntry($"########## Update #{_updatesCount++} Complete ... ##########", LoggingLevel.Info);
                _logger.WriteEntry($"       {summary.ToString()}", LoggingLevel.Info);
            }
            
        }
        
        /// <summary>
        /// Executes an action and waits for a certain time  
        /// </summary>
        /// <param name="action">Action to be executed</param>
        /// <param name="delayInMs">Time to wait before returning after execution</param>
        private void ExecuteAndWait(Action action, int delayInMs = 1000)
        {
            _watch.Restart();

            action.Invoke();
            
            //_logger.WriteEntry($"Elapsed : {_watch.ElapsedMilliseconds} Ms", LoggingLevel.Debug);

            if (_watch.ElapsedMilliseconds >= delayInMs) //took more then one sec
                return;//don't wait
            else//wait the difference
            {
                Thread.Sleep(delayInMs - Convert.ToInt32(_watch.ElapsedMilliseconds));
            }
        }

        private AmazonProduct GetAmazonProductForAsin(BagsContext dbContext, string asin)
        {
            try
            {
                return dbContext.AmazonProducts
                                .Include(pr=>pr.Product)
                                .FirstOrDefault(p => p.Product.Asin.Equals(asin, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.WriteEntry($"@GetAmazonProductForAsin | @{asin} : {ex.GetLastErrorMessage()}", LoggingLevel.Error);
            }

            return null;
        }

        private void UpdateAmazonProduct(BagsContext dbContext, Product dbProd, int qty)
        {
            var pr = GetAmazonProductForAsin(dbContext, dbProd.Asin);
            if (pr == null)//it means that this is a new product and must be inserted into the AmazonProduct table
            {
                dbContext.AmazonProducts.Add(new AmazonProduct
                {
                    LastChecked = DateTime.Now,
                    Quantity = qty,
                    Product = dbProd
                });
            }
            else//update qty and last checked date
            {
                pr.LastChecked = DateTime.Now;
                pr.Quantity = qty;
            }
        }

    }
}
