using System;
using System.Globalization;
using System.IO;
using System.Threading;

namespace LogWatch.Features.Formats {
    public abstract class ScanBase : IScanner {
        public abstract string Text { get; }
        public Action<long, int> OffsetCallback { get; set; }

        public abstract int Parse(CancellationToken cancellationToken);

        public abstract void Begin();
        public abstract Stream Source { set; }

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
        }

        public DateTime? TextAsTimestamp(string format) {
            DateTime timestamp;

            DateTime.TryParseExact(this.Text, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out timestamp);

            return timestamp;
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