using LogWatch.Annotations;

namespace LogWatch.Features.Search {
    public partial class SearchView {
        public SearchView() {
            this.InitializeComponent();
        }

        [UsedImplicitly]
        public void OnFind() {
            this.Query.Focus();
            this.Query.SelectAll();
        }
    }
}