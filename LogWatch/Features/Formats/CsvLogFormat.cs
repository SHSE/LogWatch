using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogWatch.Features.Formats {
    public class CsvLogFormat : ILogFormat {
        private bool isHeader = true;

        public CsvLogFormat() {
            this.Encoding = Encoding.UTF8;
            this.Delimeter = ';';
            this.Quote = '"';
            this.ReadHeader = true;
        }

        [LogFormatFactory("CSV")]
        public static ILogFormatFactory Factory {
            get { return new SimpleLogFormatFactory<CsvLogFormat>(CanRead); }
        }

        public Tuple<int, string>[] AttributeMappings { get; set; }
        public Encoding Encoding { get; set; }
        public char? Delimeter { get; set; }
        public char Quote { get; set; }
        public int? TimestampFieldIndex { get; set; }
        public int? LoggerFieldIndex { get; set; }
        public int? LevelFieldIndex { get; set; }
        public int? MessageFieldIndex { get; set; }
        public int? ExceptionFieldIndex { get; set; }
        public int? FieldCount { get; set; }
        public bool ReadHeader { get; set; }

        public Record DeserializeRecord(ArraySegment<byte> segment) {
            var text = Encoding.UTF8.GetString(segment.Array, segment.Offset, segment.Count);
            var fields = this.GetFields(text);

            var attributes = this.AttributeMappings;

            var record = new Record {
                Logger = SafeGetFieldByIndex(fields, this.LoggerFieldIndex),
                Level = this.GetLevel(fields),
                Message = SafeGetFieldByIndex(fields, this.MessageFieldIndex),
                Exception = SafeGetFieldByIndex(fields, this.ExceptionFieldIndex)
            };

            DateTime timestamp;

            if (DateTime.TryParse(SafeGetFieldByIndex(fields, this.TimestampFieldIndex), out timestamp))
                record.Timestamp = timestamp;

            if (attributes != null)
                record.Attributes = attributes
                    .Select(x => new KeyValuePair<string, string>(x.Item2, SafeGetFieldByIndex(fields, x.Item1)))
                    .ToArray();

            return record;
        }

        public async Task<long> ReadSegments(
            IObserver<RecordSegment> observer,
            Stream stream,
            CancellationToken cancellationToken) {
            using (var reader = new StreamReader(stream, this.Encoding, false, 4096, true)) {
                var offset = stream.Position;

                if (this.isHeader && this.ReadHeader) {
                    var header = await reader.ReadLineAsync();

                    if (header == null)
                        return offset;

                    this.GuessDelimeter(header);

                    var headerFields = this.GetFields(header);

                    this.GuessFieldMappings(headerFields);
                    this.FieldCount = headerFields.Count;

                    offset += this.Encoding.GetByteCount(header) + 2;

                    this.isHeader = false;
                }

                while (true) {
                    cancellationToken.ThrowIfCancellationRequested();

                    var length = await this.GetEndOffset(reader, cancellationToken);

                    if (length == -1)
                        return offset;

                    observer.OnNext(new RecordSegment(offset, length));

                    offset += length;
                }
            }
        }

        private static bool CanRead(Stream stream) {
            using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, true)) {
                var buffer = new char[4*1024];
                var count = reader.Read(buffer, 0, buffer.Length);
                var text = new string(buffer, 0, count);

                if (string.IsNullOrWhiteSpace(text))
                    return false;

                var lines = text.Split(new[] {'\n'}, 2, StringSplitOptions.RemoveEmptyEntries);

                var firstLine = lines.FirstOrDefault();

                return firstLine != null &&
                       firstLine.Split(new[] {';', ',', '|'}, StringSplitOptions.RemoveEmptyEntries)
                                .Select(x => x.ToLower())
                                .Any(x => x == "level" || x == "message");
            }
        }

        private void GuessDelimeter(string header) {
            var variants = new[] {';', ','};

            this.Delimeter = variants.OrderByDescending(delimeter => header.Split(delimeter).Length).First();
        }

        private LogLevel? GetLevel(IReadOnlyList<string> fields) {
            var level = SafeGetFieldByIndex(fields, this.LevelFieldIndex).ToLower();

            switch (level) {
                case "trace":
                    return LogLevel.Trace;
                case "debug":
                    return LogLevel.Debug;
                case "info":
                    return LogLevel.Info;
                case "warn":
                    return LogLevel.Warn;
                case "error":
                    return LogLevel.Error;
                case "fatal":
                    return LogLevel.Fatal;
                default:
                    return null;
            }
        }

        private static string SafeGetFieldByIndex(IReadOnlyList<string> fields, int? index) {
            if (index == null)
                return string.Empty;

            return index < fields.Count ? fields[index.Value] : string.Empty;
        }

        private void GuessFieldMappings(IEnumerable<string> headerFields) {
            var attributes = new List<Tuple<int, string>>();

            foreach (var fieldWithIndex in headerFields.Select((field, index) => new {field, index}))
                switch (fieldWithIndex.field.ToLower()) {
                    case "time":
                        this.TimestampFieldIndex = fieldWithIndex.index;
                        break;

                    case "logger":
                        this.LoggerFieldIndex = fieldWithIndex.index;
                        break;

                    case "level":
                        this.LevelFieldIndex = fieldWithIndex.index;
                        break;

                    case "message":
                        this.MessageFieldIndex = fieldWithIndex.index;
                        break;

                    case "exception":
                        this.ExceptionFieldIndex = fieldWithIndex.index;
                        break;

                    default:
                        attributes.Add(Tuple.Create(fieldWithIndex.index, fieldWithIndex.field));
                        break;
                }

            if (attributes.Count > 0)
                this.AttributeMappings = attributes.ToArray();
        }

        private IReadOnlyList<string> GetFields(string record) {
            var delimeter = this.Delimeter;
            var fields = new List<string>();
            var field = new StringBuilder(record.Length);
            var quouted = false;
            var length = record.Length;

            if (length >= 2) {
                var ending = record.Substring(length - 2);

                switch (ending) {
                    case "\r":
                    case "\n":
                        length -= 1;
                        break;

                    case "\r\n":
                        length -= 2;
                        break;
                }
            }

            for (var i = 0; i < length; i++) {
                var c = record[i];

                if (c == this.Quote)
                    quouted = !quouted;

                else if ((c == delimeter || c == '\r') && !quouted) {
                    TrimQuotes(field);
                    fields.Add(field.ToString());
                    field.Clear();
                    continue;
                }

                field.Append(c);
            }

            TrimQuotes(field);

            if (!quouted)
                fields.Add(field.ToString());

            return fields;
        }

        private static void TrimQuotes(StringBuilder field) {
            if (field.Length > 0 && field[0] == '"')
                field.Remove(0, 1);

            if (field.Length > 0 && field[field.Length - 1] == '"')
                field.Remove(field.Length - 1, 1);
        }

        private async Task<int> GetEndOffset(StreamReader reader, CancellationToken cancellationToken) {
            var position = 0;
            var newLineBytesCount = this.Encoding.GetByteCount(Environment.NewLine);
            var expectedFieldCount = this.FieldCount;
            var fieldCount = 0;
            var quote = this.Quote;
            var quoted = false;
            var delimeter = this.Delimeter;

            while (true) {
                cancellationToken.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync();

                if (line == null)
                    break;

                for (var i = 0; i < line.Length; i++) {
                    var c = line[i];

                    if (c == quote)
                        quoted = !quoted;

                    else if (c == delimeter && !quoted)
                        fieldCount++;
                }

                position += this.Encoding.GetByteCount(line) + newLineBytesCount;

                if (!quoted) {
                    fieldCount++;

                    if (fieldCount != expectedFieldCount)
                        return -1;

                    return position;
                }
            }

            if (!quoted) {
                fieldCount++;

                if (fieldCount != expectedFieldCount)
                    return -1;

                return position;
            }

            return -1;
        }
    }
}