namespace LogWatch.Features.Sources {
    public struct LogSourceStatus {
        public readonly int Count;
        public readonly bool IsProcessingSavedData;
        public readonly int Progress;

        public LogSourceStatus(int count, bool isProcessingSavedData, int progress) {
            this.Count = count;
            this.IsProcessingSavedData = isProcessingSavedData;
            this.Progress = progress;
        }
    }
}