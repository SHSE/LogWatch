using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LogWatch.Features.Formats {
    public class LexLogFormat : ILogFormat {
        private ThreadLocal<IScanner> recordsScanner;
        private Type recordsScannerType;

        public LexLogFormat() {
            this.TaskScheduler = TaskScheduler.Default;
        }

        public Type SegmentsScannerType { get; set; }

        public Type RecordsScannerType {
            get { return this.recordsScannerType; }
            set {
                this.recordsScannerType = value;
                this.recordsScanner =
                    new ThreadLocal<IScanner>(() => (IScanner) Activator.CreateInstance(this.recordsScannerType));
            }
        }

        public TaskScheduler TaskScheduler { get; set; }

        public Record DeserializeRecord(ArraySegment<byte> segment) {
            var scanner = this.recordsScanner.Value;

            scanner.Begin();
            scanner.Reset();
            scanner.Source = new MemoryStream(segment.Array, segment.Offset, segment.Count);
            scanner.Parse(CancellationToken.None);

            return new Record {
                Level = GetLevel(scanner.Level),
                Logger = scanner.Logger,
                Message = scanner.Message,
                Exception = scanner.Exception,
                Timestamp = scanner.Timestamp,
                Thread = scanner.Thread
            };
        }

        public Task<long> ReadSegments(
            IObserver<RecordSegment> observer,
            Stream stream,
            CancellationToken cancellationToken) {
            return Task.Factory.StartNew(() => {
                var scanner = (IScanner) Activator.CreateInstance(this.SegmentsScannerType);
                var initialOffset = stream.Position;
                var lastOffset = initialOffset;

                scanner.OffsetCallback = (offset, length) => {
                    if (length > 0) {
                        var segment = new RecordSegment(initialOffset + offset, length);
                        lastOffset = initialOffset + offset + length;
                        observer.OnNext(segment);
                    }
                };

                scanner.Source = new InternalStreamWrapper(stream, stream.Position);
                scanner.Parse(cancellationToken);

                return lastOffset;
            }, cancellationToken,
                TaskCreationOptions.LongRunning,
                this.TaskScheduler);
        }

        private static LogLevel? GetLevel(string levelString) {
            if (levelString == null)
                return null;

            switch (levelString.ToLower()) {
                case "trace":
                    return LogLevel.Trace;
                case "debug":
                    return LogLevel.Debug;
                case "info":
                    return LogLevel.Info;
                case "warn":
                    return LogLevel.Warn;
                case "error":
                    return LogLevel.Error;
                case "fatal":
                    return LogLevel.Fatal;
                default:
                    return null;
            }
        }

        private class InternalStreamWrapper : Stream {
            private readonly long startPosition;
            private readonly Stream stream;

            public InternalStreamWrapper(Stream stream, long startPosition) {
                this.stream = stream;
                this.startPosition = startPosition;
            }

            public override bool CanRead {
                get { return this.stream.CanRead; }
            }

            public override bool CanSeek {
                get { return this.stream.CanSeek; }
            }

            public override bool CanWrite {
                get { return false; }
            }

            public override long Length {
                get { return this.stream.Length - this.startPosition; }
            }

            public override long Position {
                get { return this.stream.Position - this.startPosition; }
                set { this.stream.Seek(value, SeekOrigin.Begin); }
            }

            public override void Flush() {
            }

            public override long Seek(long offset, SeekOrigin origin) {
                switch (origin) {
                    case SeekOrigin.Begin:
                        return this.stream.Seek(this.startPosition + offset, origin);

                    case SeekOrigin.End:
                    case SeekOrigin.Current:
                        return this.stream.Seek(offset, origin);
                }

                return -1;
            }

            public override void SetLength(long value) {
            }

            public override int Read(byte[] buffer, int offset, int count) {
                return this.stream.Read(buffer, offset, count);
            }

            public override void Write(byte[] buffer, int offset, int count) {
            }
        }
    }
}