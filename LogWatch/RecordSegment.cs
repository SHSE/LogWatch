namespace LogWatch {
    public struct RecordSegment {
        public readonly int Length;
        public readonly long Offset;

        public RecordSegment(long offset, int length) : this() {
            Offset = offset;
            Length = length;
        }

        public long End {
            get { return Offset + Length; }
        }
    }
}