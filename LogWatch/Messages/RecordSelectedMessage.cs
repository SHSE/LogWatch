namespace LogWatch.Messages {
    public sealed class RecordSelectedMessage {
        public Record Record { get; private set; }

        public RecordSelectedMessage(Record record) {
            this.Record = record;
        }
    }
}