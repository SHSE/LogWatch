using GalaSoft.MvvmLight.Messaging;
using LogWatch.Features.RecordDetails;
using LogWatch.Features.SelectSource;
using LogWatch.Messages;
using Xunit;

namespace LogWatch.Tests.ViewModels {
    public class RecordDetailsViewModelTests {
        private readonly TestMessenger messenger;
        private readonly RecordDetailsViewModel viewModel;

        public RecordDetailsViewModelTests() {
            this.messenger = new TestMessenger();

            Messenger.OverrideDefault(this.messenger);

            this.viewModel = new RecordDetailsViewModel();
        }

        [Fact]
        public void ShowsSelectedRecordDetails() {
            this.messenger.Send(new RecordSelectedMessage(new Record {Index = 7}));

            Assert.NotNull(this.viewModel.Record);
            Assert.Equal(7, this.viewModel.Record.Index);
        }
    }

    public class SelectSourceViewModelTests {
        private SelectSourceViewModel viewModel;

        public SelectSourceViewModelTests() {
            this.viewModel = new SelectSourceViewModel();
        }
    }
}