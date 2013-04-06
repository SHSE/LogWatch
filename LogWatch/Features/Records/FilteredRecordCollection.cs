using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogWatch.Sources;

namespace LogWatch.Features.Records {
    public class FilteredRecordCollection : RecordCollection {
        private readonly Predicate<Record> filter;
        private readonly ConcurrentDictionary<int, int> localIndexByRecordIndex = new ConcurrentDictionary<int, int>();
        private readonly ConcurrentDictionary<int, int> recordIndexByLocalIndex = new ConcurrentDictionary<int, int>();

        public FilteredRecordCollection(ILogSource logSource, Predicate<Record> filter) : base(logSource) {
            this.filter = filter;
        }

        public int GetLocalIndex(int recordIndex) {
            int localIndex;
            if (this.localIndexByRecordIndex.TryGetValue(recordIndex, out localIndex))
                return localIndex;
            return -1;
        }

        protected override IDisposable UpdateState() {
            var sampler = Observable
                .Return(0L, this.Scheduler)
                .Delay(TimeSpan.FromMilliseconds(100), this.Scheduler)
                .Concat(Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(2), this.Scheduler));

            return this.LogSource.Records
                       .Buffer(sampler)
                       .Where(batch => batch.Count > 0)
                       .Select(batch => new {
                           Filtered = batch.AsParallel().Where(record => this.filter(record)).ToArray(),
                           Progress = batch.Max(record => record.SourceStatus.Progress)
                       })
                       .ObserveOn(new SynchronizationContextScheduler(SynchronizationContext.Current))
                       .Subscribe(x => {
                           this.Progress = x.Progress;

                           var batch = x.Filtered;

                           if (batch.Length > 0) {
                               var filteredIndex = this.Count;
                               var isProcessingSavedData = false;

                               foreach (var record in batch) {
                                   isProcessingSavedData = record.SourceStatus.IsProcessingSavedData;
                                   this.recordIndexByLocalIndex.TryAdd(filteredIndex, record.Index);
                                   this.localIndexByRecordIndex.TryAdd(record.Index, filteredIndex);
                                   filteredIndex++;
                               }

                               this.Count += batch.Length;
                               this.IsProcessingSavedData = isProcessingSavedData;
                               this.OnCollectionReset();
                           }
                       });
        }

        protected override async Task<Record> LoadRecordAsync(int index) {
            int actualIndex;

            Record record;

            if (this.recordIndexByLocalIndex.TryGetValue(index, out actualIndex))
                record = await this.LogSource.ReadRecordAsync(actualIndex, this.CancellationToken) ?? new Record();
            else
                record = new Record();

            record.Index = index;
            record.DisplayIndex = actualIndex;

            return record;
        }
    }
}