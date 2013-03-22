namespace LogWatch.Features.SelectSource {
    public partial class SelectSourceView {
        public SelectSourceView() {
            this.InitializeComponent();
        }

        public SelectSourceViewModel ViewModel {
            get { return (SelectSourceViewModel) this.DataContext; }
        }
    }
}