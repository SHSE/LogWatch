using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;

namespace LogWatch.Features.Formats {
    public class AutoLogFormatSelector {
        [ImportMany(typeof (ILogFormatFactory))]
        public IEnumerable<Lazy<ILogFormatFactory, ILogFormatMetadata>> Formats { get; set; }

        public IEnumerable<Lazy<ILogFormatFactory, ILogFormatMetadata>> SelectFormat(Stream stream) {
            foreach (var format in this.Formats) {
                stream.Position = 0;

                if (format.Value.CanRead(stream))
                    yield return format;
            }
        }
    }
}