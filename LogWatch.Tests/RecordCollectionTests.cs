using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using LogWatch.Features.Records;
using LogWatch.Features.Sources;
using Microsoft.Reactive.Testing;
using Moq;
using Xunit;

namespace LogWatch.Tests {
    public class RecordCollectionTests {
        private readonly RecordCollection collection;
        private readonly Mock<ILogSource> logSource;
        private readonly Subject<LogSourceStatus> status = new Subject<LogSourceStatus>();
        private readonly TestScheduler testScheduler;

        public RecordCollectionTests() {
            SynchronizationContext.SetSynchronizationContext(new TestSynchronizationContext());

            this.testScheduler = new TestScheduler();
            this.logSource = new Mock<ILogSource>(MockBehavior.Strict);

            this.logSource.SetupGet(x => x.Status).Returns(this.status.ObserveOn(this.testScheduler));

            this.collection = new RecordCollection(this.logSource.Object) {Scheduler = this.testScheduler};
            this.collection.Initialize();
        }

        [Fact]
        public void LoadsRecord() {
            this.logSource
                .Setup(x => x.ReadRecordAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns((int index, CancellationToken token) => Task.FromResult(new Record {Index = index}));

            this.status.OnNext(new LogSourceStatus(2, true, 100));

            this.testScheduler.AdvanceBy(TimeSpan.FromMinutes(5).Ticks);

            var task = this.collection.GetRecordAsync(1, CancellationToken.None);

            this.testScheduler.AdvanceBy(TimeSpan.FromMinutes(5).Ticks);

            Assert.True(task.Result.IsLoaded);
            Assert.Equal(1, task.Result.Index);
        }

        [Fact(Timeout = 30000)]
        public void LoadsRequestedRecords() {
            this.logSource
                .Setup(x => x.ReadRecordAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(
                    (int index, CancellationToken cancellationToken) => Task.FromResult(new Record {Index = index}));

            this.status.OnNext(new LogSourceStatus(2, true, 100));
            
            this.testScheduler.AdvanceBy(TimeSpan.FromMinutes(5).Ticks);

            var record1 = this.collection[0];
            var record2 = this.collection[1];

            var loaded = this.collection.LoadingRecordCount.Where(x => x == 0).FirstAsync().ToTask();

            this.testScheduler.AdvanceBy(TimeSpan.FromMinutes(5).Ticks);

            loaded.Wait();

            Assert.True(record1.IsLoaded);
            Assert.True(record2.IsLoaded);
        }

        [Fact]
        public void UpdatesLoadingRecordCount() {
            var completionSource = new TaskCompletionSource<Record>();

            this.logSource
                .Setup(x => x.ReadRecordAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(completionSource.Task);

            this.status.OnNext(new LogSourceStatus(2, true, 100));
            
            this.testScheduler.AdvanceBy(TimeSpan.FromMinutes(5).Ticks);

            var record = this.collection[1];

            Assert.NotNull(record);

            var count = this.collection.LoadingRecordCount.FirstAsync().ToTask().Result;

            Assert.Equal(1, count);
        }

        [Fact]
        public void UpdatesStatus() {
            this.status.OnNext(new LogSourceStatus(7, true, 20));

            this.testScheduler.AdvanceBy(TimeSpan.FromMinutes(1).Ticks);

            Assert.Equal(7, this.collection.Count);
            Assert.True(this.collection.IsProcessingSavedData);
            Assert.Equal(20, this.collection.Progress);
        }
    }
}