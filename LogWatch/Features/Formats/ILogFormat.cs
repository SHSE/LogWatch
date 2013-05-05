using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LogWatch.Features.Formats {
    public interface ILogFormat {
        Record DeserializeRecord(ArraySegment<byte> segment);

        Task<long> ReadSegments(
            IObserver<RecordSegment> observer,
            Stream stream,
            CancellationToken cancellationToken);
    }
}