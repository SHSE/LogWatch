using System;
using System.IO;
using LogWatch.Features.Formats;
using Microsoft.Win32;

namespace LogWatch.Features.Sources {
    [LogSourceFactory("File")]
    public class FileLogSourceFactory : ILogSourceFactory{
        public LogSourceInfo Create(Func<Stream, ILogFormat> formatResolver) {
            var dialog = new OpenFileDialog {CheckFileExists = true, Multiselect = false};

            if (dialog.ShowDialog() != true)
                return null;

            var filePath = dialog.FileName;

            return Create(formatResolver, filePath);
        }

        public LogSourceInfo Create(Func<Stream, ILogFormat> formatResolver, string filePath) {
            ILogFormat format;

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                format = formatResolver(stream);

            return new LogSourceInfo(
                new FileLogSource(filePath, format),
                filePath,
                false,
                true);
        }
    }
}