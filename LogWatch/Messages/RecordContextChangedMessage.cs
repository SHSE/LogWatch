namespace LogWatch.Messages {
    public sealed class RecordContextChangedMessage {
        public RecordContextChangedMessage(Record fromRecord, Record toRecord) {
            this.FromRecord = fromRecord;
            this.ToRecord = toRecord;
        }

        public Record FromRecord { get; private set; }
        public Record ToRecord { get; private set; }
    }
}