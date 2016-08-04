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

namespace application.Synchronization
{
    //System.Timers ??
    public abstract class SynchronizerBase : ISynchronizer
    {
        #region Properties
        public string Id { get; set; }
        public string Name { get;  set; }
        protected AmazonWebClient _amazonClient;
        protected bool _reportProgress;
        protected TimeSpan _interval;
        protected TimeSpan _intervalDefault = TimeSpan.FromHours(5);
        protected bool _isIdle = true;
        protected CancellationToken _cancelToken;
        protected static readonly object _lock = new object();
        protected Timer _timer;
        protected bool _disposed;
        protected Stopwatch _watch;
        protected ISyncLogger _logger;
        protected Predicate<object> _stopWhen;
        protected Configuration _config;
        protected int _updatesCount = 1;
        protected int _productsPerBatch = 100;//be carefull if new products come in between batches !!! ==> if they do then they are up to date because just added | Or get products by last updated
        public bool IsRunning { get { return !_isIdle; } }
        #endregion
        
        public SynchronizerBase(Configuration config)
        {
            _config = config;
            _logger = new ConsoleLogger();
            _amazonClient = new AmazonWebClient(_config.AmazonAccessKey, _config.AmazonSecretKey, _config.AmazonAssociateTag);
            _watch = new Stopwatch();
            _logger.WriteEntry("Synchronization System Initialized ...", LoggingLevel.Info);
        }
        
        public virtual Task Start(CancellationToken cancelToken)
        {
          return  Task.Factory.StartNew((obj) =>
            {
                _logger.WriteEntry("Synchronization System Started ...", LoggingLevel.Info);

                _cancelToken = cancelToken;

                CheckConfigAndSetDefaults();

                _timer = new Timer(ExecuteUpdate, null, (int)Math.Ceiling(_interval.TotalMilliseconds), Timeout.Infinite);

                ExecuteUpdate(null);

                while (!_stopWhen.Invoke(null))
                {
                    if (cancelToken.IsCancellationRequested)
                        break;

                    Thread.Sleep(1000);
                }

            }, TaskCreationOptions.LongRunning, cancelToken);

        }

        public virtual void Stop()
        {
            _timer.Dispose();
            _isIdle = true;
            _logger.WriteEntry("Synchronization System Stopped ...", LoggingLevel.Info);
        }

        public virtual void Pause()
        {
            _logger.WriteEntry("Synchronization System Paused ...", LoggingLevel.Info);
        }


        public virtual SynchronizerBase WithInterval(TimeSpan interval)
        {
            _interval = interval;
            return this;
        }

        public virtual SynchronizerBase StopWhen(Predicate<object> condition)
        {
            _stopWhen = condition;
            return this;
        }
        public virtual SynchronizerBase SetProgressReportingTo(bool reportProgress)
        {
            _reportProgress = reportProgress;
            return this;
        }

        public SynchronizerBase SetLogger(ISyncLogger logger)
        {
            if(logger != null)//CheckConfigAndSetDefaults will set to defaults anyway
                _logger = logger;

            return this;
        }

        public SynchronizerBase SetBatchSize(int size)
        {
            if (size > 0)
                _productsPerBatch = size;

            return this;
        }

        protected virtual void CheckConfigAndSetDefaults()
        {
            if (_logger == null)
                _logger = new ConsoleLogger();

            if (_interval.TotalMilliseconds.Equals(0)) //interval was set to 0 or not at all
            {
                _interval = _intervalDefault;//set default delay between updates

                _logger.WriteEntry($"Update Interval was set to its default value ({_intervalDefault} hours)", LoggingLevel.Info);
            }

            //check amazon keys
            //check dbcontext
        }

        protected virtual void ExecuteUpdate(Object state)
        {
            if (!_isIdle) return;//if still updating products do nothing

            _isIdle = false;//set the sync manager state to active

            ExecuteUpdateInternal();

            //reset the timer
            _timer.Change((int)Math.Ceiling(_interval.TotalMilliseconds), Timeout.Infinite);
            _isIdle = true;
        }

        protected abstract void ExecuteUpdateInternal();

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
