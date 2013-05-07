using System.IO;

namespace LogWatch.Features.Formats {
    public interface ILogFormatFactory {
        ILogFormat Create(Stream stream);
        bool CanRead(Stream stream);
    }
}