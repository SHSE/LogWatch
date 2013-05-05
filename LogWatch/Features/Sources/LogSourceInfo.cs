namespace LogWatch.Features.Sources {
    public sealed class LogSourceInfo {
        public LogSourceInfo(ILogSource source, string name, bool autoScroll, bool collectStatsOnDemand) {
            this.CollectStatsOnDemand = collectStatsOnDemand;
            this.AutoScroll = autoScroll;
            this.Name = name;
            this.Source = source;
        }

        public ILogSource Source { get; private set; }
        public string Name { get; private set; }
        public bool AutoScroll { get; private set; }
        public bool CollectStatsOnDemand { get; private set; }
    }
}