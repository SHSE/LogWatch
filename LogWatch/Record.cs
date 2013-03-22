using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight;
using LogWatch.Sources;

namespace LogWatch {
    public sealed class Record : ObservableObject {
        private string exception;
        private LogLevel? level;
        private string logger;
        private string message;
        private string thread;
        private DateTime? timestamp;
        public int Index { get; set; }

        public DateTime? Timestamp {
            get { return this.timestamp; }
            set { this.Set(ref this.timestamp, value); }
        }

        public LogLevel? Level {
            get { return this.level; }
            set { this.Set(ref this.level, value); }
        }

        public string Logger {
            get { return this.logger; }
            set { this.Set(ref this.logger, value); }
        }

        public string Thread {
            get { return this.thread; }
            set { this.Set(ref this.thread, value); }
        }

        public string Message {
            get { return this.message; }
            set { this.Set(ref this.message, value); }
        }

        public string Exception {
            get { return this.exception; }
            set { this.Set(ref this.exception, value); }
        }

        public bool IsLoaded { get; set; }

        public LogSourceStatus SourceStatus { get; set; }

        public KeyValuePair<string, string>[] Attributes { get; set; }

        private void Set<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null) {
            this.Set(propertyName, ref field, newValue);
        }
    }
}