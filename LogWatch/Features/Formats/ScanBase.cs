using System;
using System.IO;
using System.Threading;

namespace LogWatch.Features.Formats {
    public abstract class ScanBase : IScanner {
        public abstract string Text { get; }
        public Action<long, int> OffsetCallback { get; set; }

        public abstract int Parse(CancellationToken cancellationToken);

        public abstract void Begin();
        public abstract Stream Source { set; }

        public string Timestamp { get; set; }
        public string Level { get; set; }
        public string Logger { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }

        public void Reset() {
            this.Timestamp = null;
            this.Level = null;
            this.Logger = null;
            this.Message = null;
            this.Exception = null;
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