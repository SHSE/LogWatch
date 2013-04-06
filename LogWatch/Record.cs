using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight;
using LogWatch.Annotations;
using LogWatch.Sources;

namespace LogWatch {
    public sealed class Record : ObservableObject {
        private int? displayIndex;
        private string exception;
        private LogLevel? level;
        private string logger;
        private string message;
        private string thread;
        private DateTime? timestamp;
        private bool isLoaded;

        public int Index { get; set; }

        public int? DisplayIndex {
            get { return this.displayIndex; }
            set { this.Set(ref this.displayIndex, value); }
        }

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

        public bool IsLoaded {
            get { return this.isLoaded; }
            set { this.Set(ref this.isLoaded, value); }
        }

        public LogSourceStatus SourceStatus { get; set; }

        public KeyValuePair<string, string>[] Attributes { get; set; }

        [NotifyPropertyChangedInvocator]
        private void Set<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null) {
            this.Set(propertyName, ref field, newValue);
        }
    }
}