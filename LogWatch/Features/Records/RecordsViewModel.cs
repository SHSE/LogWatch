using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LogWatch.Annotations;
using LogWatch.Messages;
using LogWatch.Sources;

namespace LogWatch.Features.Records {
    public sealed class RecordsViewModel : ViewModelBase {
        private bool autoScroll;
        private bool isFilterActive;
        private RecordCollection records;
        private TimestampFormat timestampFormat;

        public RecordsViewModel() {
            if (this.IsInDesignMode)
                return;

            this.Scheduler = System.Reactive.Concurrency.Scheduler.Default;
            this.SelectRecordCommand = new RelayCommand<Record>(this.SelectRecord);
            this.SetTimestampFormatCommand = new RelayCommand<TimestampFormat>(format => this.TimestampFormat = format);

            this.MessengerInstance.Register<NavigatedToRecordMessage>(this, this.OnNavigateToRecord);
            this.MessengerInstance.Register<RecordFilterChangedMessage>(this, this.OnFilterChanged);
        }

        public RecordCollection Records {
            get { return this.records; }
            private set { this.Set(ref this.records, value); }
        }

        public bool AutoScroll {
            get { return this.autoScroll; }
            set { this.Set(ref this.autoScroll, value); }
        }

        public RelayCommand<Record> SelectRecordCommand { get; set; }

        public IObservable<VisibleItemsInfo> RecordContext {
            set {
                if (value == null)
                    return;

                value.Where(x => x.FirstItem != null && x.LastItem != null)
                     .Sample(TimeSpan.FromMilliseconds(300), this.Scheduler)
                     .ObserveOn(new SynchronizationContextScheduler(SynchronizationContext.Current))
                     .Subscribe(
                         info =>
                         this.MessengerInstance.Send(
                             new RecordContextChangedMessage(
                             (Record) info.FirstItem,
                             (Record) info.LastItem)));
            }
        }

        public IScheduler Scheduler { get; set; }
        public LogSourceInfo LogSourceInfo { get; set; }

        public TimestampFormat TimestampFormat {
            get { return this.timestampFormat; }
            set { this.Set(ref this.timestampFormat, value); }
        }

        public RelayCommand<TimestampFormat> SetTimestampFormatCommand { get; set; }

        private void OnFilterChanged(RecordFilterChangedMessage message) {
            var oldCollection = this.records;

            if (!this.isFilterActive && message.Filter == null)
                return;

            if (oldCollection != null)
                oldCollection.Dispose();

            this.isFilterActive = message.Filter != null;

            var newCollection =
                this.isFilterActive
                    ? new FilteredRecordCollection(this.LogSourceInfo.Source, message.Filter)
                    : new RecordCollection(this.LogSourceInfo.Source);

            newCollection.Initialize();

            this.Records = newCollection;
        }

        private void Set<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null) {
            this.Set(propertyName, ref field, newValue, false);
        }

        private void SelectRecord(Record record) {
            this.MessengerInstance.Send(new RecordSelectedMessage(record));
        }

        public void Initialize() {
            this.Records = new RecordCollection(this.LogSourceInfo.Source) {Scheduler = this.Scheduler};
            this.Records.Initialize();
            this.AutoScroll = this.LogSourceInfo.AutoScroll;
        }

        private void OnNavigateToRecord(NavigatedToRecordMessage message) {
            this.AutoScroll = false;
            this.Navigated(this, new GoToIndexEventArgs(message.Index));
        }

        [UsedImplicitly]
        public event EventHandler Navigated = (sender, args) => { };
    }
}