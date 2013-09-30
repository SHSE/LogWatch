using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LogWatch.Annotations;
using LogWatch.Features.Sources;
using LogWatch.Messages;

namespace LogWatch.Features.Records {
    public sealed class RecordsViewModel : ViewModelBase {
        private bool autoScroll;
        private bool isFilterActive;
        private RecordCollection records;
        private Record selectedRecord;
        private TimestampFormat timestampFormat;

        public RecordsViewModel() {
            if (this.IsInDesignMode)
                return;

            this.Scheduler = System.Reactive.Concurrency.Scheduler.Default;
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

        public IScheduler Scheduler { get; set; }
        public LogSourceInfo LogSourceInfo { get; set; }

        public TimestampFormat TimestampFormat {
            get { return this.timestampFormat; }
            set { this.Set(ref this.timestampFormat, value); }
        }

        public RelayCommand<TimestampFormat> SetTimestampFormatCommand { get; set; }

        public Record SelectedRecord {
            get { return this.selectedRecord; }
            set {
                if (this.Set(ref this.selectedRecord, value))
                    this.MessengerInstance.Send(new RecordSelectedMessage(value));
            }
        }

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

        private bool Set<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null) {
            return this.Set(propertyName, ref field, newValue, false);
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