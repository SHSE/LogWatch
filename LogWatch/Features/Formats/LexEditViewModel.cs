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
    public class LexEditViewModel : ViewModelBase {
        private readonly LexCompiler compiler;
        private readonly LexLogFormat format;

        private bool isBusy;
        private bool isCompiled;
        private Stream logStream;
        private string logText;
        private string output;
        private string name;
        private bool save;

        public LexEditViewModel() {
            this.RunCommand = new RelayCommand(this.Preview, () => this.isBusy == false);

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
            this.compiler = new LexCompiler();
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

        public string Name {
            get { return this.name; }
            set {
                if (value == this.name)
                    return;
                this.name = value;
                this.OnPropertyChanged();
            }
        }

        public bool Save {
            get { return this.save; }
            set {
                if (value.Equals(this.save))
                    return;
                this.save = value;
                this.OnPropertyChanged();
            }
        }

        public bool IsBusy {
            get { return this.isBusy; }
            set {
                if (value.Equals(this.isBusy))
                    return;
                
                this.isBusy = value;
                this.OnPropertyChanged();
                this.RunCommand.RaiseCanExecuteChanged();
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
                this.OnPropertyChanged();
            }
        }

        public string Output {
            get { return this.output; }
            set {
                if (value == this.output)
                    return;
                this.output = value;
                this.OnPropertyChanged();
            }
        }

        public RelayCommand RunCommand { get; set; }

        public bool IsCompiled {
            get { return this.isCompiled; }
            set {
                if (value.Equals(this.isCompiled))
                    return;
                this.isCompiled = value;
                this.OnPropertyChanged();
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
            this.Output = "Running...";

            var segmentsCode = new StringBuilder();
            segmentsCode.AppendLine(this.CommonCode.Text);
            segmentsCode.AppendLine();
            segmentsCode.AppendLine(this.SegmentCode.Text);

            var recordsCode = new StringBuilder();
            recordsCode.AppendLine(this.CommonCode.Text);
            recordsCode.AppendLine();
            recordsCode.AppendLine(this.RecordCode.Text);

            var diagnostics = new StringWriter();

            this.compiler.Diagnostics = diagnostics;

            var scanners = await Task.Run(() => this.compiler.Compile(segmentsCode.ToString(), recordsCode.ToString()));

            if (scanners == null || scanners.RecordsScannerType == null || scanners.SegmentsScannerType == null) {
                this.IsBusy = false;
                this.IsCompiled = false;
                this.Output = diagnostics.ToString();
                return;
            }

            this.format.SegmentsScannerType = scanners.SegmentsScannerType;
            this.format.RecordsScannerType = scanners.RecordsScannerType;

            var stream = this.LogStream;

            stream.Position = 0;

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
            this.IsCompiled = true;
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            base.RaisePropertyChanged(propertyName);
        }
    }
}