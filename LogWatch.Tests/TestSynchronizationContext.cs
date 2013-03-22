using System.Threading;

namespace LogWatch.Tests {
    public class TestSynchronizationContext : SynchronizationContext {
        public override void Post(SendOrPostCallback d, object state) {
            d(state);
        }

        public override void Send(SendOrPostCallback d, object state) {
            d(state);
        }
    }
}