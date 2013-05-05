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
    public class FilteredRecordCollectionTests {
        private readonly FilteredRecordCollection collection;
        private readonly Mock<ILogSource> logSource;
        private readonly Subject<LogSourceStatus> status = new Subject<LogSourceStatus>();
        private readonly Subject<Record> records = new Subject<Record>();
        private readonly TestScheduler testScheduler;

        private Predicate<Record> filter;

        public FilteredRecordCollectionTests() {
            SynchronizationContext.SetSynchronizationContext(new TestSynchronizationContext());

            this.testScheduler = new TestScheduler();
            this.logSource = new Mock<ILogSource>(MockBehavior.Strict);

            this.logSource.SetupGet(x => x.Status).Returns(this.status.ObserveOn(this.testScheduler));
            this.logSource.SetupGet(x => x.Records).Returns(this.records.ObserveOn(this.testScheduler));

            this.collection = new FilteredRecordCollection(this.logSource.Object, record => this.filter(record)) {
                Scheduler = this.testScheduler
            };

            this.collection.Initialize();
        }

        [Fact]
        public void LoadsFilteredRecord() {
            this.logSource
                .Setup(x => x.ReadRecordAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new Record {Index = 1}));

            this.filter = record => record.Message == "B";

            this.records.OnNext(new Record {Index = 0, Message = "A"});
            this.records.OnNext(new Record {Index = 1, Message = "B"});

            this.testScheduler.AdvanceBy(TimeSpan.FromMinutes(5).Ticks);

            var task = this.collection.GetRecordAsync(0, CancellationToken.None);

            this.testScheduler.AdvanceBy(TimeSpan.FromMinutes(5).Ticks);

            Assert.Equal(0, task.Result.Index);
        }

        [Fact(Timeout = 30000)]
        public void LoadsRequestedFilteredRecords() {
            this.logSource
                .Setup(x => x.ReadRecordAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(
                    (int index, CancellationToken cancellationToken) => Task.FromResult(new Record {Index = index}));

            this.filter = record => record.Message == "B";

            this.records.OnNext(new Record {Index = 3, Message = "A"});
            this.records.OnNext(new Record {Index = 4, Message = "B"});
            this.records.OnNext(new Record {Index = 5, Message = "B"});

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
        public void UpdatesStatus() {
             this.filter = record => record.Message == "B";

            this.records.OnNext(new Record {Index = 3, Message = "A", SourceStatus = new LogSourceStatus(10, true, 20)});
            this.records.OnNext(new Record {Index = 4, Message = "B", SourceStatus = new LogSourceStatus(10, true, 21)});
            this.records.OnNext(new Record {Index = 5, Message = "B", SourceStatus = new LogSourceStatus(10, true, 22)});

            this.testScheduler.AdvanceBy(TimeSpan.FromMinutes(5).Ticks);

            Assert.Equal(2, this.collection.Count);
            Assert.True(this.collection.IsProcessingSavedData);
            Assert.Equal(22, this.collection.Progress);
        }
    }
}