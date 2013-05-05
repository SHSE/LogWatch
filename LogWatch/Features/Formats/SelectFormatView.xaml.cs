namespace LogWatch.Features.Formats {
    public partial class SelectFormatView {
        public SelectFormatView() {
            this.InitializeComponent();
        }

        public SelectFormatViewModel ViewModel {
            get { return (SelectFormatViewModel) this.DataContext; }
        }
    }
}