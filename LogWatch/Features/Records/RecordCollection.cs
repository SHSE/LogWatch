using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using LogWatch.Sources;

namespace LogWatch.Features.Records {
    public class RecordCollection :
        ObservableObject,
        IList<Record>,
        IList,
        IReadOnlyList<Record>,
        INotifyCollectionChanged,
        IDisposable {
        private readonly ConcurrentDictionary<int, Record> cache;
        private readonly ConcurrentQueue<int> cacheSlots;
        private readonly NotifyCollectionChangedEventArgs collectionResetArgs;
        private readonly ReplaySubject<int> loadingRecordCountSubject = new ReplaySubject<int>(1);
        private readonly ILogSource logSource;
        private readonly Subject<int> requestedResocords = new Subject<int>();
        private readonly CancellationTokenSource tokenSource;
        private readonly TaskScheduler uiScheduler;
        
        private int count;
        private bool isDisposed;
        private bool isInitialized;
        private bool isProcessingSavedData;
        private IDisposable loadRecords;
        private int loadingRecordCount;
        private int progress;
        private IDisposable updateState;

        public RecordCollection(ILogSource logSource) {
            this.CacheSize = 512;
            this.logSource = logSource;
            this.tokenSource = new CancellationTokenSource();
            this.collectionResetArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            this.cache = new ConcurrentDictionary<int, Record>();
            this.cacheSlots = new ConcurrentQueue<int>();
            this.uiScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            this.Scheduler = System.Reactive.Concurrency.Scheduler.Default;
        }

        protected ILogSource LogSource {
            get { return this.logSource; }
        }

        protected CancellationToken CancellationToken {
            get { return this.tokenSource.Token; }
        }

        public int CacheSize { get; set; }

        public int Progress {
            get { return this.progress; }
            set { this.Set(ref this.progress, value); }
        }

        public bool IsProcessingSavedData {
            get { return this.isProcessingSavedData; }
            set { this.Set(ref this.isProcessingSavedData, value); }
        }

        public IObservable<int> LoadingRecordCount {
            get { return this.loadingRecordCountSubject; }
        }

        public IScheduler Scheduler { get; set; }

        public void Dispose() {
            if (this.isDisposed)
                return;

            this.isDisposed = true;
            this.tokenSource.Cancel();
            this.updateState.Dispose();
            this.loadRecords.Dispose();

            GC.SuppressFinalize(this);
        }

        public int Add(object value) {
            throw new NotSupportedException();
        }

        public bool Contains(object value) {
            return this.Contains((Record) value);
        }

        void IList.Clear() {
            throw new NotSupportedException();
        }

        public int IndexOf(object value) {
            return this.IndexOf((Record) value);
        }

        public void Insert(int index, object value) {
            throw new NotSupportedException();
        }

        public void Remove(object value) {
            throw new NotSupportedException();
        }

        void IList.RemoveAt(int index) {
            throw new NotSupportedException();
        }

        object IList.this[int index] {
            get { return this[index]; }
            set { throw new NotSupportedException(); }
        }

        bool IList.IsReadOnly {
            get { return true; }
        }

        public bool IsFixedSize {
            get { return false; }
        }

        public void CopyTo(Array array, int index) {
            throw new NotSupportedException();
        }

        public int Count {
            get { return this.count; }
            set { this.Set(ref this.count, value); }
        }

        public object SyncRoot {
            get { return this; }
        }

        public bool IsSynchronized {
            get { return false; }
        }

        public IEnumerator<Record> GetEnumerator() {
            return Enumerable.Empty<Record>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }

        public void Add(Record item) {
            throw new NotSupportedException();
        }

        void ICollection<Record>.Clear() {
            throw new NotSupportedException();
        }

        public bool Contains(Record item) {
            if (item == null)
                return false;
            return item.Index < this.Count;
        }

        public void CopyTo(Record[] array, int arrayIndex) {
            throw new NotSupportedException();
        }

        public bool Remove(Record item) {
            throw new NotSupportedException();
        }

        int ICollection<Record>.Count {
            get { return this.Count; }
        }

        bool ICollection<Record>.IsReadOnly {
            get { return true; }
        }

        public int IndexOf(Record item) {
            if (item == null || !this.Contains(item))
                return -1;

            return item.Index;
        }

        public void Insert(int index, Record item) {
            throw new NotSupportedException();
        }

        void IList<Record>.RemoveAt(int index) {
            throw new NotSupportedException();
        }

        public Record this[int index] {
            get {
                if (index >= this.count)
                    return null;

                var isNew = false;

                var record = this.cache.GetOrAdd(index, key => {
                    isNew = true;
                    return new Record {Index = key};
                });

                if (isNew) {
                    lock (this.cacheSlots)
                        this.cacheSlots.Enqueue(index);
                    this.RequestRecord(index);
                }

                return record;
            }
            set { throw new NotSupportedException(); }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public void Initialize() {
            if (this.isInitialized)
                throw new InvalidOperationException("Already initialized");

            this.updateState = this.UpdateState();

            this.loadRecords =
                this.requestedResocords
                    .Buffer(TimeSpan.FromMilliseconds(300), this.Scheduler)
                    .Where(batch => batch.Count > 0)
                    .Subscribe(this.LoadRecordsAsync);

            this.isInitialized = true;
        }

        protected virtual IDisposable UpdateState() {
            var sampler = Observable
                .Return(0L, this.Scheduler)
                .Delay(TimeSpan.FromMilliseconds(100), this.Scheduler)
                .Concat(Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(2), this.Scheduler));

            return this.logSource.Status
                       .Sample(sampler)
                       .Where(x => x.Count > 0)
                       .ObserveOn(new SynchronizationContextScheduler(SynchronizationContext.Current))
                       .Subscribe(this.OnStatusChanged);
        }

        private void OnStatusChanged(LogSourceStatus status) {
            this.Count = status.Count;
            this.IsProcessingSavedData = status.IsProcessingSavedData;
            this.Progress = status.Progress;
            this.OnCollectionReset();
        }

        protected void OnCollectionReset() {
            var handler = this.CollectionChanged;
            if (handler != null)
                handler(this, this.collectionResetArgs);
        }

        private void Set<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null) {
            this.Set(propertyName, ref field, newValue);
        }

        private async void LoadRecordsAsync(IList<int> batch) {
            if (batch.Count > this.CacheSize)
                for (var i = this.CacheSize; i < batch.Count; i++)
                    this.loadingRecordCountSubject.OnNext(Interlocked.Decrement(ref this.loadingRecordCount));

            batch = batch.Reverse().Take(this.CacheSize).Reverse().ToArray();

            foreach (var index in batch) {
                var loaded = await this.LoadRecordAsync(index);

                loaded.IsLoaded = true;

                var isUpdated = false;

                var record = this.cache.AddOrUpdate(index, loaded, (key, existed) => {
                    if (existed.IsLoaded)
                        return existed;

                    existed.IsLoaded = true;
                    isUpdated = true;

                    return existed;
                });

                if (isUpdated)
                    await Task.Factory.StartNew(() => {
                        record.Message = loaded.Message;
                        record.Level = loaded.Level;
                        record.Logger = loaded.Logger;
                        record.Exception = loaded.Exception;
                        record.Timestamp = loaded.Timestamp;
                        record.Attributes = loaded.Attributes;
                        record.DisplayIndex = loaded.DisplayIndex;
                    }, this.CancellationToken,
                        TaskCreationOptions.None,
                        this.uiScheduler);

                this.loadingRecordCountSubject.OnNext(Interlocked.Decrement(ref this.loadingRecordCount));
            }

            this.CleanCache();
        }

        protected virtual async Task<Record> LoadRecordAsync(int index) {
            var record = await this.logSource.ReadRecordAsync(index, this.tokenSource.Token) ?? new Record {Index = index};

            record.DisplayIndex = index;

            return record;
        }

        private void RequestRecord(int index) {
            this.loadingRecordCountSubject.OnNext(Interlocked.Increment(ref this.loadingRecordCount));
            this.requestedResocords.OnNext(index);
        }

        public Task<Record> GetRecordAsync(int index, CancellationToken cancellationToken) {
            if (index >= this.count)
                return Task.FromResult<Record>(null);

            var record = this[index];

            var tcs = new TaskCompletionSource<Record>(cancellationToken);

            PropertyChangedEventHandler handler = null;

            handler = (sender, args) => {
                record.PropertyChanged -= handler;

                if (args.PropertyName == "IsLoaded" && record.IsLoaded)
                    tcs.TrySetResult(record);
            };

            record.PropertyChanged += handler;

            if (record.IsLoaded)
                tcs.TrySetResult(record);

            return tcs.Task;
        }

        private void CleanCache() {
            lock (this.cacheSlots) {
                int index;

                while (this.cache.Count > this.CacheSize)
                    if (this.cacheSlots.TryDequeue(out index)) {
                        Record _;
                        this.cache.TryRemove(index, out _);
                    } else if (this.cacheSlots.Count == 0 && this.cache.Count > 0)
                        Debugger.Break();
            }
        }

        ~RecordCollection() {
            this.Dispose();
        }
    }
}