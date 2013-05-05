using System.IO;

namespace LogWatch.Features.Formats {
    public interface ILogFormatFactory {
        ILogFormat Create();
        bool CanRead(Stream stream);
    }
}