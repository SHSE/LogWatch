using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using LogWatch.Features.Formats;
using Microsoft.Reactive.Testing;
using Xunit;

namespace LogWatch.Tests.Formats {
    public class Log4JXmlFormatTests {
        [Fact]
        public void ReadsSegmentsCorrectly() {
            var bytes = Encoding.UTF8.GetBytes(
                "<log4j:event logger=\"ConsoleApplication1.Program\" level=\"INFO\" timestamp=\"1361281966733\" thread=\"1\">" +
                "  <log4j:message>Istcua orojurf bysgurnl t.</log4j:message>" +
                "  <log4j:properties>" +
                "    <log4j:data name=\"log4japp\" value=\"ConsoleApplication1.exe(6512)\" />" +
                "    <log4j:data name=\"log4jmachinename\" value=\"user1\" />" +
                "  </log4j:properties>" +
                "</log4j:event>" +
                "<log4j:event logger=\"ConsoleApplication1.Program\" level=\"WARN\" timestamp=\"1361281966808\" thread=\"1\">" +
                "  <log4j:message>Ebo ohow aco inldrfb pameenegy.</log4j:message>" +
                "  <log4j:properties>" +
                "    <log4j:data name=\"log4japp\" value=\"ConsoleApplication1.exe(6512)\" />" +
                "    <log4j:data name=\"log4jmachinename\" value=\"user2\" />" +
                "  </log4j:properties>" +
                "</log4j:event>");

            var stream = new MemoryStream(bytes);
            var format = new Log4JXmlLogFormat();
            var testScheduler = new TestScheduler();
            var observer = testScheduler.CreateObserver<RecordSegment>();

            var offset = format.ReadSegments(observer, stream, CancellationToken.None).Result;

            Assert.Equal(2, observer.Messages.Count);

            var segments = observer.Messages.Select(x => x.Value.Value).ToArray();

            Assert.True(segments.Any(x => x.Offset == 0 && x.Length == 342));
            Assert.True(segments.Any(x => x.Offset == 342 && x.Length == 347));
            Assert.Equal(stream.Length, offset);
        }

        [Fact]
        public void ReadsSegmentsFromLargeSource() {
            var log = Enumerable
                .Repeat(
                    "<log4j:event logger=\"ConsoleApplication1.Program\" level=\"INFO\" timestamp=\"1361281966733\" thread=\"1\">" +
                    "  <log4j:message>Istcua orojurf bysgurnl t.</log4j:message>" +
                    "  <log4j:properties>" +
                    "    <log4j:data name=\"log4japp\" value=\"ConsoleApplication1.exe(6512)\" />" +
                    "    <log4j:data name=\"log4jmachinename\" value=\"user1\" />" +
                    "  </log4j:properties>" +
                    "</log4j:event>", 1200)
                .Aggregate(string.Concat);

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(log));
            var format = new Log4JXmlLogFormat {BufferSize = 10000};
            var testScheduler = new TestScheduler();
            var observer = testScheduler.CreateObserver<RecordSegment>();

            format.ReadSegments(observer, stream, CancellationToken.None).Wait();

            Assert.Equal(1200, observer.Messages.Count);

            var segments = observer.Messages.Select(x => x.Value.Value).ToArray();

            Assert.True(segments.All(x => x.Length > 0));
        }

        [Fact]
        public void DeserializesRecord() {
            var bytes = Encoding.UTF8.GetBytes(
                "<log4j:event logger=\"ConsoleApplication1.Program\" level=\"INFO\" timestamp=\"1361281966733\" thread=\"1\">" +
                "  <log4j:message>Istcua orojurf bysgurnl t.</log4j:message>" +
                "  <log4j:properties>" +
                "    <log4j:data name=\"log4japp\" value=\"ConsoleApplication1.exe(6512)\" />" +
                "    <log4j:data name=\"log4jmachinename\" value=\"user1\" />" +
                "    <log4j:data name=\"exception\" value=\"TestException\" />" +
                "  </log4j:properties>" +
                "</log4j:event>");

            var format = new Log4JXmlLogFormat();

            var record = format.DeserializeRecord(new ArraySegment<byte>(bytes));

            Assert.Equal(LogLevel.Info, record.Level);
            Assert.Equal("Istcua orojurf bysgurnl t.", record.Message);
            Assert.Equal("ConsoleApplication1.Program", record.Logger);
            Assert.Equal("1", record.Thread);
            Assert.Equal("TestException", record.Exception);
            Assert.Equal(new DateTime(2013, 02, 19, 13, 52, 47), record.Timestamp);
        }
    }
}