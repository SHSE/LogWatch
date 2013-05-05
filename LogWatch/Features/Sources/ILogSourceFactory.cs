using System;
using System.IO;
using LogWatch.Features.Formats;

namespace LogWatch.Features.Sources {
    public interface ILogSourceFactory {
        LogSourceInfo Create(Func<Stream, ILogFormat> formatResolver);
    }
}