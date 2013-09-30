using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using GalaSoft.MvvmLight.Messaging;
using LogWatch.Features.Records;
using LogWatch.Features.Sources;
using LogWatch.Messages;
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
            this.viewModel.SelectedRecord = new Record {Index = 1};

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
    }
}