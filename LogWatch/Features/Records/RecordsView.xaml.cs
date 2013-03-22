using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Windows.Input;
using LogWatch.Annotations;

namespace LogWatch.Features.Records {
    [UsedImplicitly]
    public partial class RecordsView {
        public RecordsView() {
            this.InitializeComponent();
        }

        private void Records_OnSizeChanged(object sender, SizeChangedEventArgs e) {
            var listView = (ListView) sender;
            var gridView = (GridView) listView.View;

            var lastColumn = gridView.Columns.Last();

            var newWidth = listView.ActualWidth -
                           gridView.Columns.Where(x => !Equals(x, lastColumn)).Sum(x => x.ActualWidth);

            lastColumn.Width = Math.Max(0, newWidth);
        }

        public RecordsViewModel ViewModel {
            get { return (RecordsViewModel) this.DataContext; }
        }
    }
}