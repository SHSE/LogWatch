using System;
using System.IO;

namespace LogWatch.Features.Formats {
    public interface IScanner {
        string Timestamp { get; }
        string Level { get; }
        string Logger { get; }
        string Message { get; }
        string Exception { get; }

        Action<long, int> OffsetCallback { set; }
        Stream Source { set; }

        int Parse();
        void Reset();
        void Begin();
    }
}