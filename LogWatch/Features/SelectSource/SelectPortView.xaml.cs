using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace LogWatch.Features.SelectSource {
    public partial class SelectPortView {
        public SelectPortView() {
            this.InitializeComponent();

            this.Buttons = new[] {this.OkButton, this.CancelButton};

            BindingOperations.SetBinding(this.OkButton, ButtonBase.CommandProperty,
                new Binding("DataContext.SelectPortCommand") {Source = this});
        }

        public SelectPortViewModel ViewModel {
            get { return (SelectPortViewModel) this.DataContext; }
        }
    }
}