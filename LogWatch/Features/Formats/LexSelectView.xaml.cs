namespace LogWatch.Features.Formats {
    public partial class LexSelectView {
        public LexSelectView() {
            this.InitializeComponent();

            this.Buttons = new[] {this.OkButton, this.CancelButton};
        }

        public LexSelectViewModel ViewModel {
            get { return (LexSelectViewModel) this.DataContext; }
        }
    }
}