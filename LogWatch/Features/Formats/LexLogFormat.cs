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
            scanner.Parse();

            return new Record {
                Level = GetLevel(scanner.Level),
                Logger = scanner.Logger,
                Message = scanner.Message,
                Exception = scanner.Exception
            };
        }

        public Task<long> ReadSegments(
            IObserver<RecordSegment> observer,
            Stream stream,
            CancellationToken cancellationToken) {
            return Task.Factory.StartNew(() => {
                var scanner = (IScanner) Activator.CreateInstance(this.SegmentsScannerType);

                scanner.OffsetCallback = (offset, length) => observer.OnNext(new RecordSegment(offset, length));

                scanner.Source = stream;
                scanner.Parse();

                return -1L;
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
    }
}