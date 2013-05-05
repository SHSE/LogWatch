using System;
using System.IO;

namespace LogWatch.Features.Formats {
    public sealed class SimpleLogFormatFactory<T> : ILogFormatFactory where T : class, ILogFormat, new() {
        private readonly Predicate<Stream> canRead;

        public SimpleLogFormatFactory(Predicate<Stream> canRead = null) {
            this.canRead = canRead;
        }

        public ILogFormat Create() {
            return new T();
        }

        public bool CanRead(Stream stream) {
            return this.canRead == null || this.canRead(stream);
        }
    }
}