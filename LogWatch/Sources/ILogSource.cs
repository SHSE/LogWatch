using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogWatch.Sources {
    public interface ILogSource : IDisposable {
        IObservable<Record> Records { get; }
        IObservable<LogSourceStatus> Status { get; }

        Task<Record> ReadRecordAsync(int index, CancellationToken cancellationToken);
    }
}