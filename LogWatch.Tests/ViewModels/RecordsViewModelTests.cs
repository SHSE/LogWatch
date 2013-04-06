using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using GalaSoft.MvvmLight.Messaging;
using LogWatch.Features.Records;
using LogWatch.Messages;
using LogWatch.Sources;
using Microsoft.Reactive.Testing;
using Moq;
using Xunit;

namespace LogWatch.Tests.ViewModels {
    public class RecordsViewModelTests {
        private readonly Mock<ILogSource> logSource;
        private readonly Subject<LogSourceStatus> status = new Subject<LogSourceStatus>();
        private readonly TestMessenger testMessenger;
        private readonly TestScheduler testScheduler;
        private readonly RecordsViewModel viewModel;

        public RecordsViewModelTests() {
            SynchronizationContext.SetSynchronizationContext(new TestSynchronizationContext());

            this.testScheduler = new TestScheduler();
            this.testMessenger = new TestMessenger();

            Messenger.OverrideDefault(this.testMessenger);

            this.logSource = new Mock<ILogSource>(MockBehavior.Strict);

            this.logSource
                .SetupGet(x => x.Status)
                .Returns(this.status.ObserveOn(this.testScheduler));

            this.viewModel = new RecordsViewModel {
                LogSourceInfo = new LogSourceInfo(this.logSource.Object, null, false, false),
                Scheduler = this.testScheduler
            };

            this.viewModel.Initialize();
        }

        [Fact]
        public void SelectsRecord() {
            this.viewModel.SelectRecordCommand.Execute(new Record {Index = 1});

            var message = this.testMessenger.SentMessages.OfType<RecordSelectedMessage>().Single();

            Assert.Equal(1, message.Record.Index);
        }

        [Fact]
        public void NavigatesToRecord() {
            var recordIndex = 0;

            this.viewModel.Navigated += (sender, args) => recordIndex = ((GoToIndexEventArgs) args).Index;

            this.testMessenger.Send(new NavigatedToRecordMessage(7));

            Assert.Equal(7, recordIndex);
        }

        [Fact]
        public void ReportsCurrentRecordContext() {
            RecordContextChangedMessage message = null;

            this.testMessenger.Register<RecordContextChangedMessage>(this, x => message = x);

            var context = new Subject<VisibleItemsInfo>();

            this.viewModel.RecordContext = context.ObserveOn(this.testScheduler);

            context.OnNext(new VisibleItemsInfo(new Record {Index = 7}, new Record {Index = 23}));

            this.testScheduler.AdvanceBy(TimeSpan.FromMinutes(5).Ticks);

            Assert.NotNull(message);
            Assert.Equal(7, message.FromRecord.Index);
            Assert.Equal(23, message.ToRecord.Index);
        }
    }
}