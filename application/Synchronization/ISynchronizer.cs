using System;
using System.Threading;
using System.Threading.Tasks;
using application.Log;

namespace application.Synchronization
{
    public interface ISynchronizer : IDisposable
    {
        string Id { get; set; }
        string Name { get; set; }
        bool IsRunning { get; }
        Task Start(CancellationToken cancelToken);
        void Stop();
        void Pause();
        SynchronizerBase WithInterval(TimeSpan interval);
        SynchronizerBase StopWhen(Predicate<object> condition);
        SynchronizerBase SetProgressReportingTo(bool reportProgress);
        SynchronizerBase SetLogger(ISyncLogger logger);
    }
}