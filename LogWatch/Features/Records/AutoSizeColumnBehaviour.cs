using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace LogWatch.Features.Records {
    public class AutoSizeColumnBehaviour : Behavior<ListView> {
        protected override void OnAttached() {
            base.OnAttached();


            this.AssociatedObject.AddHandler(ScrollViewer.ScrollChangedEvent, (RoutedEventHandler) OnScrollChanged);
        }

        public int ColumnIndex { get; set; }

        void OnScrollChanged(object sender, RoutedEventArgs args) {
            var gridView = this.AssociatedObject.View as GridView;

            if(gridView == null)
                return;

            var column = gridView.Columns[ColumnIndex];

            
        }
    }
}