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
    //errors in each update/ or only llast update
    //nbre of updates
    //update request ==> update response
    //reporting level ==> debug/info/warn/error
    //amazon throws exceptions
    public class SyncManager : IDisposable//<T> where T : class
    {
        #region Properties
        private BagsContext _dbContext;
        private AmazonWebClient _amazonClient;
        private bool _reportProgress;
        private TimeSpan _interval;
        private bool _isIdle = true;
        private CancellationTokenSource _cancellationToken; 
        private static readonly object _lock = new object();
        private Timer _timer;
        private bool _disposed;
        private Stopwatch _watch;
        private ISyncLogger _logger;
        public bool IsRunning { get { return !_isIdle;} }
        #endregion

        #region Ctors
        public SyncManager(Configuration config)
        {
            _logger = new ConsoleLogger();
            _amazonClient = new AmazonWebClient(config.AmazonAccessKey, config.AmazonSecretKey, config.AmazonAssociateTag);
            _dbContext = new BagsContext(config);
            var serviceProvider = _dbContext.GetInfrastructure<IServiceProvider>();
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddConsole(LogLevel.Debug);

            _logger.WriteEntry("Synchronization System Initialized ...", LoggingLevel.Info);
        }
        
        public SyncManager(BagsContext dbContext, AmazonWebClient amazonClient)
        {
            _dbContext = dbContext;
            _amazonClient = amazonClient;
        }
        public SyncManager(BagsContext dbContext, AmazonWebClient amazonClient, CancellationTokenSource cancellationToken)
            : this(dbContext, amazonClient)
        {
            _cancellationToken = cancellationToken;
        } 
        #endregion

        public void Start()
        {
            _logger.WriteEntry("Synchronization System Started ...", LoggingLevel.Info);

            CheckConfigAndSetDefaults();
            _timer = new Timer(ExecuteUpdate, null, (int)Math.Ceiling(_interval.TotalMilliseconds), Timeout.Infinite);
            ExecuteUpdate(null);
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

        public SyncManager SetProgressReportingTo(bool reportProgress)
        {
            _reportProgress = reportProgress;
            return this;
        }
        private void CheckConfigAndSetDefaults()
        {
            
        }
        
        private void ExecuteUpdate(Object state)
        {
            if (!_isIdle) return;//if still updating products do nothing
                
            //get all the products at once ==> if product count is high it could take time
            //get products by last updated
            var products = _dbContext.Products.ToList();//to do try/catch
            for (int i = 0; i < products.Count; i++)
            {
                var dbProd = products[i];

                try
                {
                    
                    //_watch.Start();
                    var amzProd = _amazonClient.GetProductSummary(dbProd.Asin).Result;
                    //what to do it product isn't available
                    //what to do it if price changes ==> up/down
                    //attributes must be added the the amazon product
                    //if analytics ==> price fluctuation/availability change rate/....

                    //_watch.Stop();
                    //_watch.ElapsedMilliseconds*1000;

                    _logger.WriteEntry($"@{dbProd.Asin} | Current Price : {dbProd.Price} |==> Amazon State : Price : {amzProd.Price} / Qty : {amzProd.Qty}", LoggingLevel.Debug);

                    if (amzProd.IsUpdateRequired(dbProd))
                    {
                        dbProd.Price = amzProd.Price;
                    }

                    Thread.Sleep(1000);//wait 1 sec ==> to do
                }
                catch (Exception ex)
                {
                    _logger.WriteEntry($"@{dbProd.Asin} : {ex.Message}", LoggingLevel.Error);
                }
            }

            _dbContext.SaveChanges();

            //reset the timer
            _timer.Change((int)Math.Ceiling(_interval.TotalMilliseconds), Timeout.Infinite);
            _isIdle = true;
            
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
