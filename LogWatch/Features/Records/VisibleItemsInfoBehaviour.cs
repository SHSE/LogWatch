using System;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Media;
using System.Linq;

namespace LogWatch.Features.Records {
    public class VisibleItemsInfoBehaviour : Behavior<ListView> {
        public static readonly DependencyProperty VisibleItemsProperty = DependencyProperty.Register(
            "VisibleItems", typeof (IObservable<VisibleItemsInfo>), typeof (VisibleItemsInfoBehaviour),
            new PropertyMetadata(null));

        private readonly Subject<VisibleItemsInfo> visibleItems = new Subject<VisibleItemsInfo>();
        private VirtualizingStackPanel virtualizingPanel;

        public IObservable<VisibleItemsInfo> VisibleItems {
            get { return (IObservable<VisibleItemsInfo>) this.GetValue(VisibleItemsProperty); }
            set { this.SetValue(VisibleItemsProperty, value); }
        }

        protected override void OnAttached() {
            base.OnAttached();

            this.VisibleItems = this.visibleItems;
            this.virtualizingPanel = this.GetVirtualizingStackPanel(this.AssociatedObject);

            this.AssociatedObject.AddHandler(ScrollViewer.ScrollChangedEvent, (RoutedEventHandler) this.OnScrollChanged);
        }

        protected override void OnDetaching() {
            base.OnDetaching();

            this.AssociatedObject.RemoveHandler(
                ScrollViewer.ScrollChangedEvent,
                (RoutedEventHandler) this.OnScrollChanged);
        }

        private VirtualizingStackPanel GetVirtualizingStackPanel(DependencyObject element) {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++) {
                var child = VisualTreeHelper.GetChild(element, i) as FrameworkElement;

                if (child == null)
                    continue;

                if (child is VirtualizingStackPanel)
                    return child as VirtualizingStackPanel;

                var panel = this.GetVirtualizingStackPanel(child);

                if (panel != null)
                    return panel;
            }

            return null;
        }

        private void OnScrollChanged(object sender, RoutedEventArgs e) {
            if (this.virtualizingPanel == null)
                this.virtualizingPanel = this.GetVirtualizingStackPanel(this.AssociatedObject);

            if (this.virtualizingPanel == null)
                return;

            var children = this.virtualizingPanel.Children;

            var firtsItem = children.OfType<FrameworkElement>().Select(x => x.DataContext).FirstOrDefault();
            var lastItem = children.OfType<FrameworkElement>()
                                   .Where((element, i) => i <= this.virtualizingPanel.ViewportHeight)
                                   .Select(x => x.DataContext)
                                   .LastOrDefault();

            this.visibleItems.OnNext(new VisibleItemsInfo(firtsItem, lastItem));
        }
    }
}