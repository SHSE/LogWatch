namespace LogWatch.Features.Records {
    public sealed class VisibleItemsInfo {
        public object FirstItem { get; private set; }
        public object LastItem { get; private set; }

        public VisibleItemsInfo(object firstItem, object lastItem) {
            this.FirstItem = firstItem;
            this.LastItem = lastItem;
        }
    }
}