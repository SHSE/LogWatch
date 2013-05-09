using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ICSharpCode.AvalonEdit.Document;
using LogWatch.Annotations;

namespace LogWatch.Features.Formats {
    public class LexViewModel : ViewModelBase {
        private readonly LexLogFormat format;

        private bool isBusy;
        private bool isReady;
        private Stream logStream;
        private string logText;
        private string output;

        public LexViewModel() {
            this.PreviewCommand = new RelayCommand(this.Preview);
            this.CommonCode = new TextDocument(
                "timestamp [^;\\r\\n]+\n" +
                "level     [^;\\r\\n]+\n" +
                "logger    [^;\\r\\n]+\n" +
                "message   [^;\\r\\n]+\n" +
                "exception [^;\\r\\n]*");

            this.SegmentCode = new TextDocument(
                "record {timestamp}[;]{message}[;]{logger}[;]{level}[;]{exception}\\r\\n\n" +
                "%%\n" +
                "{record} Segment();");

            this.RecordCode = new TextDocument(
                "%x MATCHED_TIMESTAMP\n" +
                "%x MATCHED_MESSAGE\n" +
                "%x MATCHED_LEVEL\n" +
                "%x MATCHED_LOGGER\n" +
                "%%\n" +
                "<INITIAL>{timestamp} Timestamp = yytext; BEGIN(MATCHED_TIMESTAMP);\n" +
                "<MATCHED_TIMESTAMP>{message} this.Message = yytext; BEGIN(MATCHED_MESSAGE);\n" +
                "<MATCHED_MESSAGE>{logger} this.Logger = yytext; BEGIN(MATCHED_LOGGER);\n" +
                "<MATCHED_LOGGER>{level} this.Level = yytext; BEGIN(MATCHED_LEVEL);\n" +
                "<MATCHED_LEVEL>{exception} this.Exception = yytext; BEGIN(INITIAL);");

            if (this.IsInDesignMode)
                return;

            this.format = new LexLogFormat();
        }

        public LexLogFormat Format {
            get { return this.format; }
        }

        public Stream LogStream {
            get { return this.logStream; }
            set {
                this.logStream = value;

                this.LoadLogText();
            }
        }

        public bool IsBusy {
            get { return this.isBusy; }
            set {
                if (value.Equals(this.isBusy))
                    return;
                this.isBusy = value;
                this.RaisePropertyChanged();
            }
        }

        public TextDocument CommonCode { get; set; }
        public TextDocument SegmentCode { get; set; }
        public TextDocument RecordCode { get; set; }

        public string LogText {
            get { return this.logText; }
            set {
                if (value == this.logText)
                    return;
                this.logText = value;
                this.RaisePropertyChanged();
            }
        }

        public string Output {
            get { return this.output; }
            set {
                if (value == this.output)
                    return;
                this.output = value;
                this.RaisePropertyChanged();
            }
        }

        public RelayCommand PreviewCommand { get; set; }

        public bool IsReady {
            get { return this.isReady; }
            set {
                if (value.Equals(this.isReady))
                    return;
                this.isReady = value;
                this.RaisePropertyChanged();
            }
        }

        private async void LoadLogText() {
            this.IsBusy = true;

            this.logStream.Position = 0;

            using (var reader = new StreamReader(this.logStream, Encoding.UTF8, true, 4096, true)) {
                var text = new StringBuilder();

                for (var i = 0; i < 5 && !reader.EndOfStream; i++)
                    text.AppendLine(await reader.ReadLineAsync());

                this.LogText = text.ToString();
            }

            this.IsBusy = false;
        }

        private async void Preview() {
            this.IsBusy = true;

            var code = new StringBuilder();

            code.AppendLine(this.CommonCode.Text);
            code.AppendLine();
            code.AppendLine(this.SegmentCode.Text);

            this.format.SegmentCode = code.ToString();

            code.Clear();

            code.AppendLine(this.CommonCode.Text);
            code.AppendLine();
            code.AppendLine(this.RecordCode.Text);

            this.format.RecordCode = code.ToString();

            var stream = this.LogStream;

            stream.Position = 0;

            if (!await Task.Run((Func<bool>) this.format.TryCompileSegmentsScanner) ||
                !await Task.Run((Func<bool>) this.format.TryCompileRecordsScanner)) {
                this.IsBusy = false;
                this.IsReady = false;
                this.Output = this.format.Diagnostics.ToString();
                this.format.Diagnostics = new StringWriter();
                return;
            }

            var segments = new List<RecordSegment>();
            var cts = new CancellationTokenSource();
            var subject = new Subject<RecordSegment>();

            subject.Subscribe(segment => {
                if (segments.Count == 5) {
                    cts.Cancel();
                    return;
                }

                segments.Add(segment);
            });

            await this.format.ReadSegments(subject, stream, cts.Token);

            var outputBuilder = new StringBuilder();

            var index = 1;
            foreach (var segment in segments) {
                stream.Position = segment.Offset;

                var buffer = new byte[segment.Length];

                await stream.ReadAsync(buffer, 0, buffer.Length);

                var record = this.format.DeserializeRecord(new ArraySegment<byte>(buffer));

                outputBuilder.AppendFormat("Record #{0}\n", index++);

                if (record.Timestamp != null)
                    outputBuilder.AppendFormat("  Timestamp: {0}\n", record.Timestamp);

                if (record.Level != null)
                    outputBuilder.AppendFormat("  Level:     {0}\n", record.Level);

                if (!string.IsNullOrEmpty(record.Logger))
                    outputBuilder.AppendFormat("  Logger:    {0}\n", record.Logger);

                if (!string.IsNullOrEmpty(record.Message))
                    outputBuilder.AppendFormat("  Message:   {0}\n", record.Message);

                if (!string.IsNullOrEmpty(record.Exception))
                    outputBuilder.AppendFormat("  Exception: {0}\n", record.Exception);

                outputBuilder.AppendLine();
            }

            this.Output = outputBuilder.ToString();

            this.IsBusy = false;
            this.IsReady = true;
        }

        [NotifyPropertyChangedInvocator]
        protected override void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            base.RaisePropertyChanged(propertyName);
        }
    }
}