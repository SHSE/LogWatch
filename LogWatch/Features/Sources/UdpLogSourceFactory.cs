using System;
using System.IO;
using System.Net;
using LogWatch.Features.Formats;

namespace LogWatch.Features.Sources {
    [LogSourceFactory("UDP")]
    public class UdpLogSourceFactory : ILogSourceFactory {
        public LogSourceInfo Create(Func<Stream, ILogFormat> formatResolver) {
            var dialog = new SelectPortView();

            if (dialog.ShowDialog() != true)
                return null;

            var endPoint = new IPEndPoint(IPAddress.Any, dialog.ViewModel.Port.GetValueOrDefault());
            var logSource = new UdpLogSource(endPoint, Path.GetTempFileName()) {SelectLogFormat = formatResolver};

            return new LogSourceInfo(
                logSource,
                string.Format("udp://{0}:{1}", endPoint.Address, endPoint.Port),
                true,
                false);
        }
    }
}