using System;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LogWatch.Formats;
using LogWatch.Sources;

namespace LogWatch.Features.SelectSource {
    public class SelectSourceViewModel : ViewModelBase {
        private readonly TaskScheduler uiSchdeuler;
        private bool? isLogSourceSelected;
        private LogSourceInfo source;

        public SelectSourceViewModel() {
            this.OpenFileCommand = new RelayCommand(this.OpenFile);
            this.ListenNetworkCommand = new RelayCommand(this.ListenNetwork);

            if (this.IsInDesignMode)
                return;

            this.uiSchdeuler = TaskScheduler.FromCurrentSynchronizationContext();

            this.SelectLogFormat = stream => null;
            this.SelectFile = () => null;
            this.CreateFileLogSourceInfo = filePath => null;
            this.SelectEndpoint = () => new IPEndPoint(IPAddress.Any, 13370);
        }

        public Func<string> SelectFile { get; set; }
        public Func<IPEndPoint> SelectEndpoint { get; set; }
        public Func<Stream, ILogFormat> SelectLogFormat { get; set; }
        public Func<string, LogSourceInfo> CreateFileLogSourceInfo { get; set; }
        public Action<Exception> HandleException { get; set; }

        public RelayCommand OpenFileCommand { get; set; }
        public RelayCommand ListenNetworkCommand { get; set; }

        public bool? IsLogSourceSelected {
            get { return this.isLogSourceSelected; }
            set { this.Set(ref this.isLogSourceSelected, value); }
        }

        public LogSourceInfo Source {
            get { return this.source; }
            set { this.Set(ref this.source, value); }
        }

        private void ListenNetwork() {
            var endPoint = this.SelectEndpoint();

            if (endPoint == null)
                return;

            var logSource = new UdpLogSource(endPoint, Path.GetTempFileName()) {
                SelectLogFormat = stream => Task.Factory.StartNew(
                    () => this.SelectLogFormat(stream),
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    this.uiSchdeuler)
            };

            this.Source =
                new LogSourceInfo(
                    logSource,
                    string.Format("udp://{0}:{1}", endPoint.Address, endPoint.Port),
                    true,
                    false);

            this.IsLogSourceSelected = true;
        }

        private void OpenFile() {
            var filePath = this.SelectFile();

            if (filePath != null)
                this.OpenFile(filePath);
        }

        private void OpenFile(string filePath) {
            this.Source = this.CreateFileLogSourceInfo(filePath);
            this.IsLogSourceSelected = true;
        }

        private void Set<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null) {
            this.Set(propertyName, ref field, newValue, false);
        }
    }
}