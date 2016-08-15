using System;
using System.Threading;
using System.Threading.Tasks;
using application.Logger;

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
    }
}