using System.Windows;

namespace LogWatch.Features.Search {
    public partial class SearchView {
        public SearchView() {
            this.InitializeComponent();
        }

        private void TextBox_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (this.IsVisible)
                this.Query.Focus();
        }
    }
}