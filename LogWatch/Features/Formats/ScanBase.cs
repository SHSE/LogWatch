using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace LogWatch.Features.Formats {
    public abstract class ScanBase : IScanner {
        public Action<long, int> OffsetCallback { get; set; }

        public abstract int Parse(CancellationToken cancellationToken);

        public abstract void Begin();

        public DateTime? Timestamp { get; set; }
        public string Level { get; set; }
        public string Logger { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
        public string Thread { get; set; }

        public void Reset() {
            this.Timestamp = null;
            this.Level = null;
            this.Logger = null;
            this.Message = null;
            this.Exception = null;
            this.Thread = null;
            this.Diagnostics = TextWriter.Null;
        }

        public abstract void SetSourceWithEncoding(Stream source, int codePage);

        public TextWriter Diagnostics { get; set; }

        public DateTime? TextAsTimestamp(string format, string culture = null) {
            DateTime timestamp;

            DateTime.TryParseExact(
                this.Text, 
                format,
                culture == null ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(culture), 
                DateTimeStyles.None, 
                out timestamp);

            return timestamp;
        }

        protected abstract string Text { get; }

        protected void Debug(string format, params object[] args) {
            this.Diagnostics.WriteLine(format, args);
        }

        protected void Debug(object obj) {
            this.Diagnostics.WriteLine(obj);
        }

        protected void NextOffset(long offset, int length) {
            this.OffsetCallback(offset, length);
        }

        public abstract int yylex();

        protected virtual bool yywrap() {
            return true;
        }
    }
}