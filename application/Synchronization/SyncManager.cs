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
    public class SyncManager : IDisposable//<T> where T : class
    {
        #region Properties
        //private BagsContext _dbContext;
        private AmazonWebClient _amazonClient;
        private bool _reportProgress;
        private TimeSpan _interval;
        private TimeSpan _intervalDefault = TimeSpan.FromHours(5);
        private bool _isIdle = true;
        private CancellationTokenSource _cancellationToken; 
        private static readonly object _lock = new object();
        private Timer _timer;
        private bool _disposed;
        private Stopwatch _watch;
        private ISyncLogger _logger;
        private Predicate<object> _stopWhen;
        private Configuration _config;
        private int _updatesCount = 1;
        private int _productsPerBatch = 100;//be carefull if new products come in between batches !!! ==> if they do then they are up to date because just added | Or get products by last updated
        public bool IsRunning { get { return !_isIdle;} }
        #endregion

        #region Ctors
        public SyncManager(Configuration config)
        {
            _config = config;
            _logger = new ConsoleLogger();
            _amazonClient = new AmazonWebClient(_config.AmazonAccessKey, _config.AmazonSecretKey, _config.AmazonAssociateTag);
            //_dbContext = new BagsContext(config);
            //var serviceProvider = _dbContext.GetInfrastructure<IServiceProvider>();
            //var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            //loggerFactory.AddConsole(LogLevel.Debug);
            _watch = new Stopwatch();
            _logger.WriteEntry("Synchronization System Initialized ...", LoggingLevel.Info);
        }
        
        //public SyncManager(BagsContext dbContext, AmazonWebClient amazonClient)
        //{
        //    _dbContext = dbContext;
        //    _amazonClient = amazonClient;
        //}
        //public SyncManager(BagsContext dbContext, AmazonWebClient amazonClient, CancellationTokenSource cancellationToken)
        //    : this(dbContext, amazonClient)
        //{
        //    _cancellationToken = cancellationToken;
        //} 
        #endregion

        public void Start()
        {
            _logger.WriteEntry("Synchronization System Started ...", LoggingLevel.Info);

            CheckConfigAndSetDefaults();
            _timer = new Timer(ExecuteUpdate, null, (int)Math.Ceiling(_interval.TotalMilliseconds), Timeout.Infinite);
            ExecuteUpdate(null);

            while (!_stopWhen.Invoke(null))
            {
                Thread.Sleep(1000);
            }
        }
        
        public void Stop()
        {
            _timer.Dispose();
            _isIdle = true;
            _logger.WriteEntry("Synchronization System Stopped ...", LoggingLevel.Info);
        }

        public void Pause()
        {
            _logger.WriteEntry("Synchronization System Paused ...", LoggingLevel.Info);
        }
        

        public SyncManager WithInterval(TimeSpan interval)
        {
            _interval = interval;
            return this;
        }

        public SyncManager StopWhen(Predicate<object> condition)
        {
            _stopWhen = condition;
            return this;
        }
        public SyncManager SetProgressReportingTo(bool reportProgress)
        {
            _reportProgress = reportProgress;
            return this;
        }
        private void CheckConfigAndSetDefaults()
        {
            if (_interval.TotalMilliseconds.Equals(0)) //interval was set to 0 on not at all
            {
                _interval = _intervalDefault;//set default delay between updates

                _logger.WriteEntry($"Update Interval was set to its default value ({_intervalDefault} hours)", LoggingLevel.Info);
            }
                
            //check amazon keys
            //check dbcontext
        }
        
        private void ExecuteUpdate(Object state)
        {
            if (!_isIdle) return;//if still updating products do nothing

            _isIdle = false;//set the sync manager state to active

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
                    if (products == null) break;
                    summary.ProductCount += products.Count;//products.Count = _productsPerBatch
                    startIndex += products.Count;

                    //_logger.WriteEntry($"{products.Count} products found...", LoggingLevel.Debug);

                    foreach (var dbProd in products)
                    {
                        try
                        {
                            //what to do if product isn't available
                            //what to do if price changes ==> up/down
                            //attributes must be added the the amazon product
                            //if analytics ==> price fluctuation/availability change rate/....

                            ExecuteAndWait(() =>
                            {
                                var amzProd = _amazonClient.GetProductSummary(dbProd.Asin).Result;
                                _logger.WriteEntry($"@Update#{_updatesCount} | @{dbProd.Asin} | Current Price : {dbProd.Price} |==> Amazon State : Price : {amzProd.Price} / Qty : {amzProd.Qty}", LoggingLevel.Debug);

                                if (amzProd.IsUpdateRequired(dbProd))
                                {
                                    dbProd.Price = amzProd.Price;
                                }

                                dbContext.SaveChangesAsync();
                                summary.UpdatedCount++;
                            });

                        }
                        catch (Exception ex)
                        {
                            _logger.WriteEntry($"@{dbProd.Asin} : {ex.Message}", LoggingLevel.Error);
                            summary.ErrorAsins.Add(dbProd.Asin);
                        }
                    }
                }
 
                summary.EndDate = DateTime.Now;
                _logger.WriteEntry($"########## Update #{_updatesCount++} Complete ... ##########", LoggingLevel.Info);
                _logger.WriteEntry($"       {summary.ToString()}", LoggingLevel.Info);
            }
            
            //reset the timer
            _timer.Change((int)Math.Ceiling(_interval.TotalMilliseconds), Timeout.Infinite);
            _isIdle = true;
            
        }

        private List<Product> FetchProductsFromDb(BagsContext dbContext, int startIndex = -1, int productCount = -1)
        {
            try
            {
                if (startIndex < 1)
                    startIndex = 1;

                //if (startIndex > 0)
                //{
                    if(productCount > 0)
                        return dbContext.Products
                                        .Skip(startIndex - 1)
                                        .Take(productCount)
                                        .ToList();
                    else
                        return dbContext.Products
                                        .Skip(startIndex - 1)
                                        .ToList();
                //}
                //else
                //{
                //    return dbContext.Products.ToList();
                //}
            }
            catch (Exception ex)
            {
                _logger.WriteEntry($"Error getting products from DB : {ex.Message}", LoggingLevel.Error);
                return null;
            }
        }

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

        #region IDisposable
        public void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                //cleanup
                Stop();
                _disposed = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        } 
        #endregion
    }
}
