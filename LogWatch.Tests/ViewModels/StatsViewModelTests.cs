using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using GalaSoft.MvvmLight.Messaging;
using LogWatch.Features.Sources;
using LogWatch.Features.Stats;
using LogWatch.Messages;
using Microsoft.Reactive.Testing;
using Moq;
using Xunit;

namespace LogWatch.Tests.ViewModels {
    public class StatsViewModelTests {
        private readonly Mock<ILogSource> logSource;
        private readonly TestMessenger messenger;
        private readonly Subject<Record> records = new Subject<Record>();
        private readonly TestScheduler testScheduler;
        private readonly StatsViewModel viewModel;

        public StatsViewModelTests() {
            SynchronizationContext.SetSynchronizationContext(new TestSynchronizationContext());

            this.testScheduler = new TestScheduler();
            this.messenger = new TestMessenger();
            this.logSource = new Mock<ILogSource>();
            this.logSource.SetupGet(x => x.Records).Returns(this.records.ObserveOn(this.testScheduler));

            Messenger.OverrideDefault(this.messenger);

            this.viewModel = new StatsViewModel {
                Scheduler = this.testScheduler,
                LogSourceInfo = new LogSourceInfo(this.logSource.Object, null, false, false)
            };
        }

        [Fact]
        public void NavigatesToRecord() {
            this.viewModel.CollectCommand.Execute(null);

            this.records.OnNext(new Record {Index = 1, Level = LogLevel.Trace});
            this.records.OnNext(new Record {Index = 2, Level = LogLevel.Debug});
            this.records.OnNext(new Record {Index = 3, Level = LogLevel.Info});
            this.records.OnNext(new Record {Index = 4, Level = LogLevel.Warn});
            this.records.OnNext(new Record {Index = 5, Level = LogLevel.Error});
            this.records.OnNext(new Record {Index = 6, Level = LogLevel.Fatal});

            this.testScheduler.AdvanceBy(TimeSpan.FromMinutes(5).Ticks);

            this.messenger.Send(
                new RecordContextChangedMessage(
                    new Record {Index = 1},
                    new Record {Index = 2}));

            this.viewModel.GoToNextCommand.Execute(LogLevel.Error);

            Assert.NotEmpty(this.messenger.SentMessages.OfType<NavigatedToRecordMessage>().Where(x => x.Index == 5));
        }

        [Fact]
        public void CollectsStats() {
            this.viewModel.CollectCommand.Execute(null);

            this.records.OnNext(new Record {Index = 1, Level = LogLevel.Trace});
            this.records.OnNext(new Record {Index = 2, Level = LogLevel.Debug});
            this.records.OnNext(new Record {Index = 3, Level = LogLevel.Info});
            this.records.OnNext(new Record {Index = 4, Level = LogLevel.Warn});
            this.records.OnNext(new Record {Index = 5, Level = LogLevel.Error});
            this.records.OnNext(new Record {Index = 6, Level = LogLevel.Fatal});

            this.testScheduler.AdvanceBy(TimeSpan.FromMinutes(5).Ticks);

            Assert.Equal(1, this.viewModel.TraceCount);
            Assert.Equal(1, this.viewModel.DebugCount);
            Assert.Equal(1, this.viewModel.InfoCount);
            Assert.Equal(1, this.viewModel.WarnCount);
            Assert.Equal(1, this.viewModel.ErrorCount);
            Assert.Equal(1, this.viewModel.FatalCount);
        }
    }
}