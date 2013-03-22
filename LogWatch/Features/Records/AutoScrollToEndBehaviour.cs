using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace LogWatch.Features.Records {
    public class AutoScrollToEndBehaviour : Behavior<ListView> {
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(
            "IsEnabled", typeof (bool), typeof (AutoScrollToEndBehaviour), new PropertyMetadata(true));

        private bool isScrolling;

        private RecordCollection recordCollection;
        private IDisposable subscription;

        public bool IsEnabled {
            get { return (bool) this.GetValue(IsEnabledProperty); }
            set { this.SetValue(IsEnabledProperty, value); }
        }

        protected override void OnAttached() {
            base.OnAttached();

            this.recordCollection = this.AssociatedObject.ItemsSource as RecordCollection;

            var descriptor = DependencyPropertyDescriptor.FromProperty(
                ItemsControl.ItemsSourceProperty,
                typeof (Control));

            descriptor.AddValueChanged(this.AssociatedObject, this.OnItemsSourceChanged);

            if (this.recordCollection != null)
                this.Subscribe();
        }

        private void OnItemsSourceChanged(object sender, EventArgs args) {
            if (this.recordCollection != null)
                this.Unsubscribe();

            this.recordCollection = this.AssociatedObject.ItemsSource as RecordCollection;

            if (this.recordCollection != null)
                this.Subscribe();
        }

        private void Subscribe() {
            this.AssociatedObject.AddHandler(ScrollViewer.ScrollChangedEvent, (RoutedEventHandler) this.OnScrollChanged);

            this.subscription =
                Observable.FromEventPattern<NotifyCollectionChangedEventArgs>(this.recordCollection, "CollectionChanged")
                          .Throttle(TimeSpan.FromMilliseconds(700))
                          .ObserveOnDispatcher()
                          .Where(_ => this.IsEnabled && !this.isScrolling)
                          .Subscribe(_ => this.OnObservableCollectionChanged());
        }

        private void OnScrollChanged(object sender, RoutedEventArgs routedEventArgs) {
            var args = (ScrollChangedEventArgs) routedEventArgs;

            if (args.VerticalChange < 0)
                this.IsEnabled = false;
        }

        protected override void OnDetaching() {
            base.OnDetaching();

            if (this.recordCollection != null)
                this.Unsubscribe();
        }

        private void Unsubscribe() {
            this.AssociatedObject.RemoveHandler(
                ScrollViewer.ScrollChangedEvent,
                (RoutedEventHandler) this.OnScrollChanged);

            this.subscription.Dispose();
        }

        private async void OnObservableCollectionChanged() {
            if (this.recordCollection.Count == 0)
                return;

            this.isScrolling = true;

            await this.recordCollection.LoadingRecordCount.Where(x => x == 0).FirstAsync();

            var lastRecord =
                await this.recordCollection.GetRecordAsync(this.recordCollection.Count - 1, CancellationToken.None);

            if (Equals(this.AssociatedObject.Items.CurrentItem, lastRecord))
                return;

            if (this.AssociatedObject.Items.MoveCurrentTo(lastRecord))
                this.AssociatedObject.ScrollIntoView(lastRecord);

            this.isScrolling = false;
        }
    }
}