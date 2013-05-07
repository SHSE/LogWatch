namespace LogWatch.Features.Formats {
    public partial class LexView {
        public LexView() {
            this.InitializeComponent();
        }

        public LexViewModel ViewModel {
            get { return (LexViewModel) this.DataContext; }
        }
    }
}