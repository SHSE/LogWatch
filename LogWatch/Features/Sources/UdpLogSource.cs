using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LogWatch.Features.Formats;

namespace LogWatch.Features.Sources {
    public class UdpLogSource : ILogSource {
        private readonly string dumpFilePath;
        private readonly FileStream dumpStream;
        private readonly IPEndPoint endPointToListen;
        private readonly ManualResetEvent fileSourceCreated = new ManualResetEvent(false);
        private readonly UdpClient udpClient;
        private FileLogSource fileSource;
        private bool isDisposed;

        public UdpLogSource(IPEndPoint endPoint, string dumpFilePath) {
            this.endPointToListen = endPoint;
            this.dumpFilePath = dumpFilePath;
            this.udpClient = new UdpClient(endPoint) {Client = {ReceiveTimeout = 1000}};
            this.dumpStream = OpenDumpStream(dumpFilePath);

            this.RecordsSeparator = Encoding.UTF8.GetBytes(Environment.NewLine);

            Task.Factory.StartNew(
                this.Listen,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public Func<Stream, ILogFormat> SelectLogFormat { get; set; }

        public byte[] RecordsSeparator { get; set; }

        public void Dispose() {
            if (this.isDisposed)
                return;

            this.isDisposed = true;

            this.udpClient.Close();
            this.dumpStream.Dispose();

            GC.SuppressFinalize(this);
        }

        public IObservable<Record> Records {
            get {
                return Observable.Defer(() => {
                    this.fileSourceCreated.WaitOne();
                    return this.fileSource.Records;
                }).SubscribeOn(TaskPoolScheduler.Default);
            }
        }

        public IObservable<LogSourceStatus> Status {
            get {
                return Observable.Defer(() => {
                    this.fileSourceCreated.WaitOne();
                    return this.fileSource.Status;
                }).SubscribeOn(TaskPoolScheduler.Default);
            }
        }

        public event Action<Exception> Error = exception => { };

        public Task<Record> ReadRecordAsync(int index, CancellationToken cancellationToken) {
             var logSource = this.fileSource;

            if (logSource == null)
                return null;

            return logSource.ReadRecordAsync(index, cancellationToken); 
        }

        private static FileStream OpenDumpStream(string dumpFilePath) {
            return new FileStream(
                dumpFilePath,
                FileMode.Open,
                FileAccess.Write,
                FileShare.Read,
                4096,
                FileOptions.WriteThrough);
        }

        private void Listen() {
            var shouldInitializeLogSource = true;

            while (!this.isDisposed) {
                var endPoint = this.endPointToListen;

                byte[] data;

                try {
                    data = this.udpClient.Receive(ref endPoint);
                } catch (ObjectDisposedException) {
                    break;
                } catch (SocketException exception) {
                    if (exception.SocketErrorCode == SocketError.TimedOut)
                        continue;

                    if (exception.SocketErrorCode == SocketError.Interrupted)
                        break;

                    this.Error(exception);
                    break;
                }

                if (data.Length > 0) {
                    this.dumpStream.Write(data, 0, data.Length);
                    this.dumpStream.Write(this.RecordsSeparator, 0, this.RecordsSeparator.Length);
                    this.dumpStream.Flush();

                    if (this.fileSource != null)
                        this.fileSource.FileChanged();

                    if (shouldInitializeLogSource) {
                        shouldInitializeLogSource = false;
                        this.InitializeLogSource();
                    }
                }
            }
        }

        ~UdpLogSource() {
            this.Dispose();
        }

        private void InitializeLogSource() {
            ILogFormat logFormat;

            using (var fileStream = File.Open(this.dumpFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                logFormat = this.SelectLogFormat(fileStream);

            this.fileSource = new FileLogSource(this.dumpFilePath, logFormat);
            this.fileSourceCreated.Set();
        }
    }
}