using System;
using System.IO;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using LogWatch.Features.Formats;
using Xunit;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace LogWatch.Tests.Formats {
    public class LexFormatTests {
        [Fact(Timeout = 60000)]
        public void ReadsSegments() {
            var stream = CreateStream("01.01.2012T15:41:23 DEBUG Hello world!\r\n02.01.2012T10:23:03 WARN Bye bye!");
            const string lex = @"
                timestamp   [0-9]{2}[.][0-9]{2}[.][0-9]{4}[T][0-9]{2}[:][0-9]{2}[:][0-9]{2}
                level       TRACE|DEBUG|INFO|WARN|ERROR|FATAL
                message     [^\r\n]+

                record      {timestamp}[ ]{level}[ ]{message}\r\n
                %%
                {record} Segment();
                ";

            var compiler = new LexCompiler {Diagnostics = Console.Out};

            var scanners = compiler.Compile(lex, "%%");

            var format = new LexLogFormat {SegmentsScannerType = scanners.SegmentsScannerType};

            var subject = new ReplaySubject<RecordSegment>();

            format.ReadSegments(subject, stream, CancellationToken.None).Wait();

            var segment = subject.FirstAsync().ToTask().Result;

            stream.Position = segment.Offset;

            var buffer = new byte[segment.Length];

            stream.Read(buffer, 0, buffer.Length);

            var str = Encoding.UTF8.GetString(buffer);

            Assert.Equal("01.01.2012T15:41:23 DEBUG Hello world!\r\n", str);
        }

        [Fact]
        public void DeserializesRecord() {
            const string lex = @"
                timestamp   [0-9]{2}[.][0-9]{2}[.][0-9]{4}[T][0-9]{2}[:][0-9]{2}[:][0-9]{2}
                level       TRACE|DEBUG|INFO|WARN|ERROR|FATAL
                message     [^\r\n]+

                %x matched_level
                %%

                <INITIAL,matched_level> {
                    {timestamp} { this.Timestamp = yytext; BEGIN(INITIAL); }
                    {level} { this.Level = yytext; BEGIN(matched_level); }
                }
                <matched_level>{message} { this.Message = yytext; BEGIN(INITIAL); }
                ";

            var compiler = new LexCompiler {Diagnostics = Console.Out};

            var scanners = compiler.Compile("%%", lex);
            
            var format = new LexLogFormat {RecordsScannerType = scanners.RecordsScannerType};

            var record = format.DeserializeRecord(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes("01.01.2012T15:41:23 DEBUG Hello world!\r\n")));

            Assert.Equal(LogLevel.Debug, record.Level);
            Assert.Equal(" Hello world!", record.Message);
        }

        private static MemoryStream CreateStream(string content) {
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);
            return stream;
        } 
    }
}