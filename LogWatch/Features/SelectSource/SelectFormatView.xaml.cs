namespace LogWatch.Features.SelectSource {
    public partial class SelectFormatView {
        public SelectFormatView() {
            this.InitializeComponent();
        }

        public SelectFormatViewModel ViewModel {
            get { return (SelectFormatViewModel) this.DataContext; }
        }
    }
}