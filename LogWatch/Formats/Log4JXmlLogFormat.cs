using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using LogWatch.Util;

namespace LogWatch.Formats {
    [LogFormat("Log4J XML")]
    public class Log4JXmlLogFormat : ILogFormat {
        private static readonly byte[] EventStart = Encoding.UTF8.GetBytes("<log4j:event ");
        private static readonly byte[] EventEnd = Encoding.UTF8.GetBytes("</log4j:event>");

        public Log4JXmlLogFormat() {
            this.BufferSize = 16*1024;
        }

        public int BufferSize { get; set; }

        public Record DeserializeRecord(ArraySegment<byte> segment) {
            try {
                var text = Encoding.UTF8.GetString(segment.Array, segment.Offset, segment.Count)
                                   .Replace("<log4j:event", "<log4j:event xmlns:log4j=\"urn:ignore\"");

                var element = XElement.Parse(text);

                XNamespace ns = "urn:ignore";

                var properties = element.Element(ns + "properties");

                return new Record {
                    Level = GetLevel((string) element.Attribute("level")),
                    Logger = (string) element.Attribute("logger"),
                    Thread = (string) element.Attribute("thread"),
                    Message = element.Elements(ns + "message").Select(x => x.Value).FirstOrDefault(),
                    Timestamp = JavaTimeStampToDateTime((long) element.Attribute("timestamp")),
                    Exception = properties == null ? null :
                                    properties.Elements(ns + "data")
                                              .Where(x => (string) x.Attribute("name") == "exception")
                                              .Select(x => (string) x.Attribute("value"))
                                              .FirstOrDefault(),
                };
            } catch (XmlException) {
                return null;
            }
        }

        public async Task<long> ReadSegments(
            IObserver<RecordSegment> observer,
            Stream stream,
            CancellationToken cancellationToken) {
            var offset = stream.Position;

            var buffer = new byte[this.BufferSize];

            while (true) {
                stream.Position = offset;

                var count = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                if (count == 0)
                    return offset;

                var startOffsets = KmpUtil.GetOccurences(
                    EventStart,
                    new ArraySegment<byte>(buffer, 0, count),
                    cancellationToken);

                var endOffsets = KmpUtil.GetOccurences(
                    EventEnd,
                    new ArraySegment<byte>(buffer, 0, count),
                    cancellationToken);

                if (startOffsets.Count == 0 || endOffsets.Count == 0)
                    return offset;

                var baseOffset = offset;
                var segments = endOffsets.Zip(
                    startOffsets.Take(endOffsets.Count),
                    (end, start) =>
                    new RecordSegment(baseOffset + start, (int) (end + EventEnd.Length - start)));

                foreach (var segment in segments) {
                    observer.OnNext(segment);
                    offset = segment.End;
                }
            }
        }

        public bool CanRead(Stream stream) {
            using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, true)) {
                var buffer = new char[4*1024];
                var count = reader.Read(buffer, 0, buffer.Length);
                var text = new string(buffer, 0, count);

                return !string.IsNullOrWhiteSpace(text) && text.Contains("<log4j:event");
            }
        }

        private static LogLevel? GetLevel(string levelString) {
            if (levelString == null)
                return null;

            switch (levelString.ToLower()) {
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

        private static DateTime JavaTimeStampToDateTime(double javaTimeStamp) {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(Math.Round(javaTimeStamp/1000));
        }
    }
}