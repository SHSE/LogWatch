using System;

namespace LogWatch.Features.Records {
    public sealed class GoToIndexEventArgs : EventArgs {
        public GoToIndexEventArgs(int index) {
            this.Index = index;
        }

        public int Index { get; private set; }
    }
}