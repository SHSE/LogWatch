using System;

namespace LogWatch.Messages {
    public sealed class RecordFilterChangedMessage {
        public Predicate<Record> Filter { get; private set; }

        public RecordFilterChangedMessage(Predicate<Record> filter) {
            this.Filter = filter;
        }
    }
}