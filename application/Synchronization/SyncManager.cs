using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using application.Log;
using Microsoft.Extensions.Primitives;

namespace application.Synchronization
{
    public class SyncManager : IDisposable
    {
        private Dictionary<string, ISynchronizer> _synchronizers;
        private Configuration _config;
        private List<Task> _runningSynchronizers;
        private CancellationTokenSource _cancelToken;
        private bool _disposed;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config"></param>
        public SyncManager(Configuration config)
        {
            if(config == null)
                throw new ArgumentNullException("Configuration cannot be null.");

            _synchronizers = new Dictionary<string, ISynchronizer>();
            _runningSynchronizers= new List<Task>(); 
            _config = config;
        }

        public ISynchronizer this[string id]
        {
            get
            {
                if (_synchronizers  == null || !_synchronizers.ContainsKey(id))
                    return null;

                return _synchronizers[id];
            }
        }

        /// <summary>
        /// Adds a new synchronizer to the pool
        /// </summary>
        /// <typeparam name="T">type of the synchronizer ==> must implement ISynchronizer </typeparam>
        /// <param name="interval">Delay between two consecutive updates</param>
        /// <param name="stopCondition">When the synchronizer should stop</param>
        /// <param name="logger">Logger use to keep track of the synchronizer activity ==> must implement ISyncLogger</param>
        /// <param name="config">Configuration used to instantiate the synchronizer ==> added in case you need to override the original config given to the syncManager ctor</param>
        /// <returns></returns>
        public SyncManager Add<T>(TimeSpan interval, Predicate<object> stopCondition, ISyncLogger logger = null, Configuration config = null) where T : ISynchronizer//, new ()
        {
            var conf = config ?? _config;
            var sync = (T)Activator.CreateInstance(typeof(T), conf);//to do
            sync.Id = Guid.NewGuid().ToString();//to do

            sync.WithInterval(interval)
                .StopWhen(stopCondition)
                .SetLogger(logger);

            _synchronizers.Add(sync.Id, sync);
            
            return this;
        }
        
        /// <summary>
        /// Remove all the synchronizers from the pool
        /// </summary>
        /// <returns></returns>
        public SyncManager RemoveAll()
        {
            StopAll();
            foreach (var sync in _synchronizers.Values)
            {
                sync.Dispose();
            }
            _synchronizers.Clear();

            return this;

        }

        /// <summary>
        /// Starts all the synchronizers
        /// </summary>
        /// <param name="cancelToken"></param>
        public void StartAll(CancellationToken cancelToken)
        {
            foreach (var synchronizer in _synchronizers.Values)//might need for loop
            {
                _runningSynchronizers.Add(synchronizer.Start(cancelToken));
            }

            Task.WaitAll(_runningSynchronizers.ToArray());
        }
        
        /// <summary>
        /// Stops all the synchronizers
        /// </summary>
        /// <returns></returns>
        public SyncManager StopAll()
        {
            foreach (var synchronizer in _synchronizers.Values)
            {
                synchronizer.Stop();
            }
            return this;
        }
  
        /// <summary>
        /// Disposes a sync
        /// </summary>
        /// <param name="id">Id of the sync</param>
        private void Dispose(string id)
        {
            var sync = this[id];
            sync?.Dispose();
        }

        /// <summary>
        /// Disposes all the syncs
        /// </summary>
        private void DisposeAll()
        {
            for (int i = 0; i < _synchronizers.Keys.Count; i++)
            {
                Dispose(_synchronizers.Keys.ElementAt(i));
            }
        }
        
        #region IDisposable
        public void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                //cleanup
                StopAll();
                DisposeAll();
                _disposed = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
        
        #region Commented
        //private SyncManager Start(string id)
        //{
        //    var sync = this[id];
        //    sync?.Start(cancelToken);
        //    return this;
        //}
        //private SyncManager Stop(string id)
        //{
        //    var sync = this[id];
        //    sync?.Stop();
        //    return this;
        //}
        //private SyncManager Remove(string id)
        //{
        //    Stop(id);
        //    Dispose(id);

        //    _synchronizers.Remove(id);

        //    return this;
        //} 
        #endregion
    }
}
