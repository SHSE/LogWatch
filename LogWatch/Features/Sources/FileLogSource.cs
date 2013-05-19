using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using LogWatch.Features.Formats;
using LogWatch.Util;

namespace LogWatch.Features.Sources {
    public class FileLogSource : ILogSource {
        private readonly AutoResetEventAsync fileChanged = new AutoResetEventAsync(false);
        private readonly string filePath;
        private readonly ILogFormat logFormat;
        private readonly Lazy<Stream> recordsStream;
        private readonly SemaphoreSlim recordsStreamSemaphore = new SemaphoreSlim(1);
        private readonly ConcurrentDictionary<int, RecordSegment> segments;
        private readonly Stream segmentsStream;
        private readonly ReplaySubject<LogSourceStatus> status = new ReplaySubject<LogSourceStatus>(1);
        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();
        private readonly FileSystemWatcher watcher;

        private bool isDisposed;
        private long streamLength;

        public FileLogSource(string filePath, ILogFormat logFormat) {
            this.BufferSize = 64*1024;

            this.filePath = filePath;

            this.watcher = this.CreateFileWatcher();
            this.watcher.Changed += (sender, args) => this.FileChanged();
            this.watcher.EnableRaisingEvents = true;

            this.segments = new ConcurrentDictionary<int, RecordSegment>();
            this.segmentsStream = OpenSegmentsStream(filePath);
            this.recordsStream = new Lazy<Stream>(this.OpenRecordStream);

            this.logFormat = logFormat;

            Task.Run(() => this.LoadSegmentsAsync(this.tokenSource.Token), this.tokenSource.Token);
        }

        public int BufferSize { get; set; }

        public IObservable<Record> Records {
            get {
                return Observable
                    .Create<Record>(
                        (observer, cancellationToken) => this.ObserveRecordsAsync(observer, cancellationToken))
                    .SubscribeOn(ThreadPoolScheduler.Instance);
            }
        }

        public void Dispose() {
            if (this.isDisposed)
                return;

            this.isDisposed = true;
            this.tokenSource.Cancel();
            this.status.Dispose();
            this.watcher.Dispose();
            if (this.recordsStream.IsValueCreated)
                this.recordsStream.Value.Dispose();
        }

        public IObservable<LogSourceStatus> Status {
            get { return this.status; }
        }

        public async Task<Record> ReadRecordAsync(int index, CancellationToken cancellationToken) {
            var stream = this.recordsStream.Value;

            RecordSegment segment;

            if (this.segments.TryGetValue(index, out segment)) {
                await this.recordsStreamSemaphore.WaitAsync(cancellationToken);

                var buffer = new byte[segment.Length];
                int count;

                try {
                    stream.Position = segment.Offset;
                    count = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                } finally {
                    this.recordsStreamSemaphore.Release();
                }

                //if (count != segment.Length)
                //    throw new ApplicationException("Segment mapping failed");

                var record = this.logFormat.DeserializeRecord(new ArraySegment<byte>(buffer, 0, count));

                if (record == null)
                    return null;

                record.Index = index;

                var currentLength = Interlocked.Read(ref this.streamLength);

                record.SourceStatus = new LogSourceStatus(
                    index + 1,
                    segment.End < currentLength,
                    (int) (100.0*segment.End/currentLength));

                return record;
            }

            return null;
        }

        private static FileStream OpenSegmentsStream(string filePath) {
            return File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public void FileChanged() {
            this.fileChanged.Set();
        }

        private async Task LoadSegmentsAsync(CancellationToken cancellationToken) {
            using (var observer = new Subject<RecordSegment>()) {
                observer.Subscribe(segment => {
                    var index = this.segments.Count;

                    if (this.segments.TryAdd(index, segment)) {
                        var length = this.streamLength;

                        if (!this.isDisposed)
                            this.status.OnNext(new LogSourceStatus(
                                index + 1,
                                segment.End < length,
                                (int) (100.0*this.segmentsStream.Position/length)));
                    }
                });

                while (!cancellationToken.IsCancellationRequested) {
                    Interlocked.Exchange(ref this.streamLength, this.segmentsStream.Length);

                    var startPosition =
                        await this.logFormat.ReadSegments(observer, this.segmentsStream, cancellationToken);

                    await this.fileChanged.WaitAsync(cancellationToken);

                    this.segmentsStream.Position = startPosition;
                }
            }
        }

        private async Task ObserveRecordsAsync(IObserver<Record> observer, CancellationToken cancellationToken) {
            var statusChanged = new AutoResetEventAsync(true);

            var index = 0;

            using (this.status.Subscribe(_ => statusChanged.Set()))
                while (!cancellationToken.IsCancellationRequested) {
                    await statusChanged.WaitAsync(cancellationToken);

                    for (; index < this.segments.Count; index++) {
                        cancellationToken.ThrowIfCancellationRequested();

                        var currentIndex = index;
                        var record = await this.ReadRecordAsync(currentIndex, cancellationToken);

                        if (record != null)
                            observer.OnNext(record);
                    }
                }
        }

        private FileSystemWatcher CreateFileWatcher() {
            var fileName = Path.GetFileName(this.filePath);

            if (fileName == null)
                throw new ArgumentException("filePath");

            var directoryName = Path.GetDirectoryName(this.filePath);

            return new FileSystemWatcher(directoryName ?? @".", fileName);
        }

        private Stream OpenRecordStream() {
            return File.Open(this.filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        ~FileLogSource() {
            this.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}