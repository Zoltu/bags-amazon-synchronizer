using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using application.Amazon;
using application.Data;
using application.Logger;
using application.Models;
using Microsoft.ApplicationInsights;

namespace application.Synchronization
{
    public abstract class SynchronizerBase : ISynchronizer
    {
        #region Properties
        public string Id { get; set; }
        public string Name { get;  set; }
        protected AmazonUtilities _amazonClient;
        protected TimeSpan _interval;
        protected TimeSpan _intervalDefault = TimeSpan.FromHours(5);
        protected bool _isIdle = true;
        protected CancellationToken _cancelToken;
        protected Timer _timer;
        protected bool _disposed;
        protected Stopwatch _watch;
        protected Predicate<object> _stopWhen;
        protected Configuration _config;
        protected TelemetryClient _telemetryClient;
        protected int _updatesCount = 1;
        protected const int _productsPerBatch = 10;//keep it at 10 for amazon api batch
        public bool IsRunning { get { return !_isIdle; } }
        
        #endregion
        
        public SynchronizerBase(Configuration config, TelemetryClient telemetryClient)
        {
            if(telemetryClient == null)
                throw new ArgumentNullException("Telemetry client cannot be null.");

            _telemetryClient = telemetryClient;
            _config = config;
            _amazonClient = new AmazonUtilities(_config.AmazonAccessKey, _config.AmazonSecretKey, _config.AmazonAssociateTag);
            _watch = new Stopwatch();
            _telemetryClient.TrackEvent("Synchronization System Initialized ...");
        }
        
        public virtual Task Start(CancellationToken cancelToken)
        {
          return  Task.Factory.StartNew((obj) =>
            {
                _telemetryClient.TrackEvent("Synchronization System Started ...");

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
            _telemetryClient.TrackEvent("Synchronization System Stopped ...");
        }

        public virtual void Pause()
        {
            _telemetryClient.TrackEvent("Synchronization System Paused ...");
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
        
        protected virtual void CheckConfigAndSetDefaults()
        {
            if (_interval.TotalMilliseconds.Equals(0)) //interval was set to 0 or not at all
            {
                _interval = _intervalDefault;//set default delay between updates

                _telemetryClient.TrackEvent("CheckConfigAndSetDefaults", new Dictionary<string, string>()
                {
                    {"UpdateIntervalDefault", $"Update Interval was set to its default value ({_intervalDefault} hours)"} 
                });
                
            }
            
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
                _amazonClient = null;
                _watch = null;
                //_telemetryClient.Flush();
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
