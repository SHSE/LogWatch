using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;

namespace LogWatch.Formats {
    public class AutoLogFormatSelector {
        [ImportMany(typeof (ILogFormat))]
        public IEnumerable<Lazy<ILogFormat, ILogFormatMetadata>> Formats { get; set; }

        public IEnumerable<Lazy<ILogFormat, ILogFormatMetadata>> SelectFormat(Stream stream) {
            foreach (var format in this.Formats) {
                stream.Position = 0;

                if (format.Value.CanRead(stream))
                    yield return format;
            }
        }
    }
}