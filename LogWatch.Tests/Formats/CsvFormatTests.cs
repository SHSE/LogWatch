using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using LogWatch.Features.Formats;
using Xunit;

namespace LogWatch.Tests.Formats {
    public class CsvFormatTests {
        [Fact]
        public void ReadsSegment() {
            var stream = CreateStream("2012-03-05 13:56:12;warn;Program;\"Test message\";TextException\r\n");

            var format = new CsvLogFormat {
                ReadHeader = false,
                Delimeter = ';',
                FieldCount = 5
            };

            var subject = new ReplaySubject<RecordSegment>();

            format.ReadSegments(subject, stream, CancellationToken.None).Wait();

            subject.OnCompleted();

            var segment = subject.ToEnumerable().FirstOrDefault();

            Assert.Equal(0, segment.Offset);
            Assert.Equal(stream.Length, segment.Length);
        }

        [Fact]
        public void GuessesOptionsFromHeader() {
            var stream1 = CreateStream("time;level;message;logger;exception");
            var stream2 = CreateStream("time,level,message,logger,exception");

            var format = new CsvLogFormat {ReadHeader = true};

            format.ReadSegments(new Subject<RecordSegment>(), stream1, CancellationToken.None).Wait();

            Assert.Equal(';', format.Delimeter);
            Assert.Equal(5, format.FieldCount);
            Assert.Equal(0, format.TimestampFieldIndex);
            Assert.Equal(1, format.LevelFieldIndex);
            Assert.Equal(2, format.MessageFieldIndex);
            Assert.Equal(3, format.LoggerFieldIndex);
            Assert.Equal(4, format.ExceptionFieldIndex);

            format = new CsvLogFormat {ReadHeader = true};
            format.ReadSegments(new Subject<RecordSegment>(), stream2, CancellationToken.None).Wait();

            Assert.Equal(',', format.Delimeter);
        }

        [Fact]
        public void ReadsSegmentWithQuotedField() {
            var stream = CreateStream(
                "time;level;message;logger;exception\r\n" +
                "2012-01-01 00:00:00;Info;\"Quoted \r\n field \r \n\";Program;Exception\r\n");

            var format = new CsvLogFormat();
            var subject = new ReplaySubject<RecordSegment>();

            format.ReadSegments(subject, stream, CancellationToken.None).Wait();

            subject.OnCompleted();

            var segment = subject.ToEnumerable().FirstOrDefault();

            Assert.Equal(37, segment.Offset);
            Assert.Equal(68, segment.Length);
        }

        [Fact]
        public void DeserializesRecord() {
            var bytes = Encoding.UTF8.GetBytes("2012-01-01 01:39:40;Info;\"Quoted \r\n field \r \n\";Program;Exception");

            var format = new CsvLogFormat {
                Delimeter = ';',
                FieldCount = 5,
                TimestampFieldIndex = 0,
                LevelFieldIndex = 1,
                MessageFieldIndex = 2,
                LoggerFieldIndex = 3,
                ExceptionFieldIndex = 4
            };

            var record = format.DeserializeRecord(new ArraySegment<byte>(bytes));

            Assert.Equal(new DateTime(2012, 1, 1, 1, 39, 40), record.Timestamp);
            Assert.Equal(LogLevel.Info, record.Level);
            Assert.Equal("Quoted \r\n field \r \n", record.Message);
            Assert.Equal("Program", record.Logger);
            Assert.Equal("Exception", record.Exception);
        }

        private static MemoryStream CreateStream(string content) {
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);
            return stream;
        }
    }
}