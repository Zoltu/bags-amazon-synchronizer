using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using application.Data;
using application.Logger;
using application.Models;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace application.Synchronization
{
    public class AmazonSynchronizer : SynchronizerBase
    {
        public AmazonSynchronizer(Configuration config, TelemetryClient telemetryClient) 
            : base(config, telemetryClient)
        {
        }
        
        protected override void ExecuteUpdateInternal()
        {
            _telemetryClient.TrackEvent("Update Started", new Dictionary<string, string>
            {
                {"Update#", _updatesCount.ToString()}
            });

           var summary = new UpdateSummary()
            {
                StartDate = DateTime.Now
            };

            var startIndex = 0;
            var count = 0;
            var isSaveRequired = false;
            var ids = GetAllIds();

            while (true)
            {
                using (var dbContext = new BagsContext(_config))
                {
                    //Getting products from DB
                    var products = GetProductsByIds(dbContext, ids.Skip(startIndex - 1).Take(_productsPerBatch).ToList());
                    if (products == null || products.Count == 0)
                        break;
                    
                    summary.ProductCount += products.Count;
                    startIndex += products.Count;
                    
                    try
                    {
                        if (_cancelToken.IsCancellationRequested)
                            return;

                        ExecuteAndWait(() =>
                        {
                            foreach (var productSummary in _amazonClient.GetProductSummary(string.Join(",", products.Select(p => p.Asin))).Result)
                            {
                                var dbProd = products.FirstOrDefault(pr => pr.Asin.Equals(productSummary.Asin));
  
                                if (!productSummary.Available)
                                {
                                    summary.UnavailableCount++;
                                }

                                if (productSummary.IsUpdateRequired(dbProd.AmazonProduct))
                                {
                                    UpdateAmazonProduct(dbContext, productSummary, dbProd);
                                    summary.UpdatedCount++;
                                    isSaveRequired = true;//specifies that the batch has been modified and needs to be saved to DB
                                }

                                _telemetryClient.TrackEvent("Product Update Status", new Dictionary<string, string>()
                                {
                                    {"Update#" ,_updatesCount.ToString()},
                                    {"Product#" ,(++count).ToString()},
                                    {"ASIN", dbProd.AmazonProduct.Asin },
                                    {"Old Price", dbProd.AmazonProduct.Price.ToString() },
                                    {"New Price", productSummary.Price.ToString() },
                                    {"Old Availability", dbProd.AmazonProduct.Available.ToString() },
                                    {"New Availability", productSummary.Available.ToString() }
                                });

                            }

                        });

                    }
                    catch (Exception ex)
                    {
                        _telemetryClient.TrackException(ex, new Dictionary<string, string>()
                        {
                            {"Source", "ExecuteUpdateInternal" },
                            {"Update#" ,_updatesCount.ToString()}
                        });
                    }

                    //a save per batch not per product
                    if (isSaveRequired)
                    {
                        dbContext.SaveChanges();
                        isSaveRequired = false;//reset to false
                    }
                        

                }//using
                
            }//while

            summary.EndDate = DateTime.Now;
            
            _telemetryClient.TrackEvent("Update Summary", new Dictionary<string, string>
            {
                {"Update#", _updatesCount.ToString()},
                {"Total Products", summary.ProductCount.ToString()},
                {"Total Updated Products", summary.UpdatedCount.ToString()},
                {"Total Unavailable Products", summary.UnavailableCount.ToString()},
                {"Total Update Errors", summary.ErrorCount.ToString()},
                {"Update Duration", summary.Duration}
            });

            _updatesCount++;
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
                _telemetryClient.TrackException(ex, new Dictionary<string, string>()
                {
                    {"Source", "ExecuteAndWait" },
                    {"Update#" ,_updatesCount.ToString()}
                });
            }

            //_logger.WriteEntry($"Took : {_watch.ElapsedMilliseconds} Ms", LoggingLevel.Debug, _updatesCount);

            if (_watch.ElapsedMilliseconds >= delayInMs) //took more then one sec
                return;//don't wait
            else//wait the difference
            {
                Thread.Sleep(delayInMs - Convert.ToInt32(_watch.ElapsedMilliseconds));
            }
        }
        
        private void UpdateAmazonProduct(BagsContext dbContext, ProductSummary prodSum, Product dbProd)
        {
            //it means that this is a new product and must be inserted into the AmazonProduct table
            if (dbProd.AmazonProduct == null)
            {
                dbContext.AmazonProducts.Add(new AmazonProduct
                {
                    Asin = prodSum.Asin,
                    Price = Convert.ToInt32(prodSum.Price),
                    LastChecked = DateTime.Now,
                    Available = prodSum.Available,
                    Product = dbProd
                });
            }
            else//update existing amazon product
            {
                dbProd.AmazonProduct.Price = Convert.ToInt32(prodSum.Price);
                dbProd.AmazonProduct.Available = prodSum.Available;
                dbProd.AmazonProduct.LastChecked = DateTime.Now;
            }
        }
        
        private List<Product> GetProductsByIds(BagsContext dbContext, List<int> ids)
        {
            try
            {
                return dbContext.Products
                                .Include(p => p.AmazonProduct)
                                .Where(pr => ids.Contains(pr.Id))//gets converted to IN clause
                                .ToList();
                
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackException(ex, new Dictionary<string, string>()
                {
                    {"Source", "GetProductsByIds" },
                    {"Update#" ,_updatesCount.ToString()}
                });

                return null;
            }
        }
        private List<int> GetAllIds()
        {
            using (var db = new BagsContext(_config))
            {
                return db.Products
                         .Select(prod => prod.Id)
                         .ToList();
            }
        }
    }
}
