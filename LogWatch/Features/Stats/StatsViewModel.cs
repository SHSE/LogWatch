using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LogWatch.Features.Sources;
using LogWatch.Messages;

namespace LogWatch.Features.Stats {
    public class StatsViewModel : ViewModelBase {
        private ImmutableList<int> debugs = ImmutableList.Create<int>();
        private ImmutableList<int> errors = ImmutableList.Create<int>();
        private ImmutableList<int> fatals = ImmutableList.Create<int>();
        private ImmutableList<int> infos = ImmutableList.Create<int>();
        private bool isColllecting;
        private bool isProcessingSavedData;
        private int lastRecordIndex;
        private int progress;
        private ImmutableList<int> traces = ImmutableList.Create<int>();
        private ImmutableList<int> warns = ImmutableList.Create<int>();

        public StatsViewModel() {
            if (this.IsInDesignMode)
                return;

            this.Scheduler = System.Reactive.Concurrency.Scheduler.Default;
            this.CollectCommand = new RelayCommand(this.Collect);
            this.GoToNextCommand = new RelayCommand<LogLevel>(this.GoToNext);

            this.MessengerInstance.Register<RecordContextChangedMessage>(this, this.OnRecordContextChanged);
        }

        public bool IsColllecting {
            get { return this.isColllecting; }
            set { this.Set(ref this.isColllecting, value); }
        }

        public bool IsProcessingSavedData {
            get { return this.isProcessingSavedData; }
            set { this.Set(ref this.isProcessingSavedData, value); }
        }

        public int Progress {
            get { return this.progress; }
            set { this.Set(ref this.progress, value); }
        }

        public int TraceCount { get; set; }
        public int DebugCount { get; set; }
        public int InfoCount { get; set; }
        public int WarnCount { get; set; }
        public int ErrorCount { get; set; }
        public int FatalCount { get; set; }

        public RelayCommand CollectCommand { get; set; }
        public RelayCommand<LogLevel> GoToNextCommand { get; set; }
        public Action<string> InfoDialog { get; set; }
        public IScheduler Scheduler { get; set; }
        public LogSourceInfo LogSourceInfo { get; set; }

        private void Set<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null) {
            this.Set(propertyName, ref field, newValue, false);
        }

        private void OnRecordContextChanged(RecordContextChangedMessage message) {
            this.lastRecordIndex = message.ToRecord.Index;
        }

        private void GoToNext(LogLevel level) {
            ImmutableList<int> indices;

            switch (level) {
                case LogLevel.Trace:
                    indices = this.traces;
                    break;

                case LogLevel.Debug:
                    indices = this.debugs;
                    break;

                case LogLevel.Info:
                    indices = this.infos;
                    break;

                case LogLevel.Warn:
                    indices = this.warns;
                    break;

                case LogLevel.Error:
                    indices = this.errors;
                    break;

                case LogLevel.Fatal:
                    indices = this.fatals;
                    break;

                default:
                    return;
            }

            if (indices.Count > 0) {
                var recordIndex = indices.Find(index => index > this.lastRecordIndex);

                if (recordIndex == 0) {
                    this.InfoDialog("No records found");
                    return;
                }

                this.lastRecordIndex = recordIndex;
                this.MessengerInstance.Send(new NavigatedToRecordMessage(recordIndex));
            }
        }

        private void Collect() {
            if (this.IsColllecting)
                return;

            this.IsColllecting = true;

            this.LogSourceInfo.Source.Records
                .Buffer(TimeSpan.FromSeconds(1), 8*1024, this.Scheduler)
                .Where(x => x.Count > 0)
                .Select(batch => {
                    var groupings = batch.GroupBy(x => x.Level).ToArray();

                    foreach (var grouping in groupings)
                        switch (grouping.Key) {
                            case LogLevel.Trace:
                                this.traces = this.traces.AddRange(grouping.Select(x => x.Index));
                                break;

                            case LogLevel.Debug:
                                this.debugs = this.debugs.AddRange(grouping.Select(x => x.Index));
                                break;

                            case LogLevel.Info:
                                this.infos = this.infos.AddRange(grouping.Select(x => x.Index));
                                break;

                            case LogLevel.Warn:
                                this.warns = this.warns.AddRange(grouping.Select(x => x.Index));
                                break;

                            case LogLevel.Error:
                                this.errors = this.errors.AddRange(grouping.Select(x => x.Index));
                                break;

                            case LogLevel.Fatal:
                                this.fatals = this.fatals.AddRange(grouping.Select(x => x.Index));
                                break;
                        }

                    return new {
                        groupings,
                        batch.Last().SourceStatus
                    };
                })
                .ObserveOn(new SynchronizationContextScheduler(SynchronizationContext.Current))
                .Subscribe(item => {
                    this.IsProcessingSavedData = item.SourceStatus.IsProcessingSavedData;
                    this.Progress = item.SourceStatus.Progress;

                    foreach (var group in item.groupings) {
                        var count = group.Count();

                        switch (group.Key) {
                            case LogLevel.Trace:
                                this.TraceCount += count;
                                break;

                            case LogLevel.Debug:
                                this.DebugCount += count;
                                break;

                            case LogLevel.Info:
                                this.InfoCount += count;
                                break;

                            case LogLevel.Warn:
                                this.WarnCount += count;
                                break;

                            case LogLevel.Error:
                                this.ErrorCount += count;
                                break;

                            case LogLevel.Fatal:
                                this.FatalCount += count;
                                break;
                        }
                    }

                    this.RaisePropertyChanged(() => this.TraceCount);
                    this.RaisePropertyChanged(() => this.DebugCount);
                    this.RaisePropertyChanged(() => this.InfoCount);
                    this.RaisePropertyChanged(() => this.WarnCount);
                    this.RaisePropertyChanged(() => this.ErrorCount);
                    this.RaisePropertyChanged(() => this.FatalCount);
                });
        }
    }
}