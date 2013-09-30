using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LogWatch.Features.Formats;
using Xunit;

namespace LogWatch.Tests.Formats {
    public class LexFormatTests {
        [Fact]
        public void ReadsSegments() {
            var stream = CreateStream("01.01.2012T15:41:23 DEBUG Hello world!\r\n02.01.2012T10:23:03 WARN Bye bye!");

            const string lex = @"
                start   [0-9]{2}[.][0-9]{2}[.][0-9]{4}[T][0-9]{2}[:][0-9]{2}[:][0-9]{2}
                end     \r\n
                %%
                {start} BeginSegment();
                {end} EndSegment();
                <<EOF>> EndSegment();
                ";

            var compiler = new LexCompiler {Diagnostics = Console.Out};

            var scanners = compiler.Compile(lex, "%%");

            Assert.True(scanners.Success);

            var taskScheduler = new TestTaskScheduler();

            var format = new LexLogFormat {
                SegmentsScannerType = scanners.SegmentsScannerType,
                Diagnostics = Console.Out,
                TaskScheduler = taskScheduler
            };

            var segments = new List<RecordSegment>();
            var subject = new Subject<RecordSegment>();

            subject.Subscribe(segments.Add);

            format.ReadSegments(subject, stream, CancellationToken.None);

            taskScheduler.ExecuteAll();

            var segment = segments.FirstOrDefault();

            Assert.NotNull(segment);

            stream.Position = segment.Offset;

            var buffer = new byte[segment.Length];

            stream.Read(buffer, 0, buffer.Length);

            var str = Encoding.UTF8.GetString(buffer);

            Assert.Equal("01.01.2012T15:41:23 DEBUG Hello world!", str);
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
                    {timestamp} { this.Timestamp = TextAsTimestamp(""MM.dd.yyyyTHH:mm:ss""); BEGIN(INITIAL); }
                    {level} { this.Level = yytext; BEGIN(matched_level); }
                }
                <matched_level>{message} { this.Message = yytext; BEGIN(INITIAL); }
                ";

            var compiler = new LexCompiler {Diagnostics = Console.Out};
            var scanners = compiler.Compile("%%", lex);
            var taskScheduler = new TestTaskScheduler();

            var format = new LexLogFormat {
                RecordsScannerType = scanners.RecordsScannerType,
                TaskScheduler = taskScheduler
            };

            var record = format.DeserializeRecord(
                new ArraySegment<byte>(Encoding.UTF8.GetBytes("01.01.2012T15:41:23 DEBUG Hello world!\r\n")));

            taskScheduler.ExecuteAll();

            Assert.Equal(LogLevel.Debug, record.Level);
            Assert.Equal(" Hello world!", record.Message);
        }

        private static MemoryStream CreateStream(string content) {
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);
            return stream;
        }

        private class TestTaskScheduler : TaskScheduler {
            private readonly Queue<Task> tasks = new Queue<Task>();

            protected override void QueueTask(Task task) {
                this.tasks.Enqueue(task);
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) {
                return this.TryExecuteTask(task);
            }

            protected override IEnumerable<Task> GetScheduledTasks() {
                return this.tasks;
            }

            public void ExecuteAll() {
                while (this.tasks.Count > 0)
                    this.TryExecuteTask(this.tasks.Dequeue());
            }
        }
    }
}