using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using application.Amazon;
using application.Data;
using application.Log;
using application.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace application.Synchronization
{
    //start with simple sync then add features
    //make it fluent API
    //updates in a dictionary
    //errors in each update/ or only last update
    //nbre of updates
    //update request ==> update response
    //reporting level ==> debug/info/warn/error
    //amazon throws exceptions
    public class AmazonSynchronizer : SynchronizerBase//<T> where T : class
    {
        public AmazonSynchronizer(Configuration config):base(config) {}
        
        protected override void ExecuteUpdateInternal()
        {
            //get all the products at once ==> if product count is high it could take time
            //get products by last updated
            using (var dbContext = new BagsContext(_config))
            {
                _logger.WriteEntry("##########################################################", LoggingLevel.Info);
                _logger.WriteEntry($"########## Update #{_updatesCount} Started ##########", LoggingLevel.Info);

                var summary = new UpdateSummary()
                {
                    //ProductCount = products.Count,
                    StartDate = DateTime.Now
                };

                var startIndex = 0;

                //_logger.WriteEntry("Fetching products from the database ...", LoggingLevel.Debug);

                while (true)//to do
                {
                    //Getting products from DB
                    var products = FetchProductsFromDb(dbContext, startIndex, _productsPerBatch);
                    if (products == null || products.Count == 0)
                        break;
                    summary.ProductCount += products.Count;//products.Count = _productsPerBatch
                    startIndex += products.Count;

                    //_logger.WriteEntry($"{products.Count} products found...", LoggingLevel.Debug);

                    foreach (var dbProd in products)
                    {
                        try
                        {
                            if (_cancelToken.IsCancellationRequested)//if cancelled ==> exit
                                return;

                            //what to do if product isn't available
                            //what to do if price changes ==> up/down
                            //attributes must be added the the amazon product
                            //if analytics ==> price fluctuation/availability change rate/....

                            ExecuteAndWait(() =>
                            {
                                var amzProd = _amazonClient.GetProductSummary(dbProd.Asin).Result;
                                if(amzProd == null) return;
                                
                                _logger.WriteEntry($"@Update#{_updatesCount} | @{dbProd.Asin} | Current Price : {dbProd.Price} |==> Amazon State : Price : {amzProd.Price} / Qty : {amzProd.Qty}", LoggingLevel.Debug);

                                if (amzProd.IsUpdateRequired(dbProd))
                                {
                                    dbProd.Price = amzProd.Price;
                                    dbContext.SaveChangesAsync();
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
                }
 
                summary.EndDate = DateTime.Now;
                _logger.WriteEntry($"########## Update #{_updatesCount++} Complete ... ##########", LoggingLevel.Info);
                _logger.WriteEntry($"       {summary.ToString()}", LoggingLevel.Info);
            }
            
        }

        private List<Product> FetchProductsFromDb(BagsContext dbContext, int startIndex = -1, int productCount = -1)
        {
            try
            {

                //if (startIndex >= _productsPerBatch) return null;

                if (startIndex < 1)
                    startIndex = 1;
                
                if(productCount > 0)
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

        /// <summary>
        /// Executes an action and waits for a certain time  
        /// </summary>
        /// <param name="action">Action to be executed</param>
        /// <param name="delayInMs">Time to wait before returning </param>
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
        
    }
}
