namespace LogWatch.Messages {
    public class NavigatedToRecordMessage {
        public int Index { get; private set; }

        public NavigatedToRecordMessage(int index) {
            Index = index;
        }
    }
}