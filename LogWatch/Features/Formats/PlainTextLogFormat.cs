using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogWatch.Features.Formats {
    public class PlainTextLogFormat : ILogFormat {
        public PlainTextLogFormat() {
            this.Encoding = Encoding.UTF8;
        }

        public Encoding Encoding { get; set; }

        public Record DeserializeRecord(ArraySegment<byte> segment) {
            return new Record {
                Message = this.Encoding.GetString(segment.Array, segment.Offset, segment.Count)
            };
        }

        public async Task<long> ReadSegments(
            IObserver<RecordSegment> observer,
            Stream stream,
            CancellationToken cancellationToken) {
            var offset = stream.Position;
            var newLineBytesCount = this.Encoding.GetByteCount(Environment.NewLine);

            using (var reader = new StreamReader(stream, this.Encoding, false, 4096, true))
                while (true) {
                    var line = await reader.ReadLineAsync();

                    if (line == null)
                        return offset;

                    if (line.Length == 0)
                        continue;

                    var length = this.Encoding.GetByteCount(line) + newLineBytesCount;

                    observer.OnNext(new RecordSegment(offset, length));

                    offset += length;
                }
        }

        public bool CanRead(Stream stream) {
            return true;
        }
    }
}