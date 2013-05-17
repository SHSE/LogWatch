using System;
using System.Windows;

namespace LogWatch.Features.Formats {
    public partial class LexPresetsView {
        public static readonly Func<bool> ConfirmDelete =
            () => ShowMessage("Are you sure?", "Delete", MessageBoxButton.YesNo) == true;

        public LexPresetsView() {
            this.InitializeComponent();

            this.Buttons = new[] {this.OkButton, this.CancelButton};
        }

        public LexPresetsViewModel ViewModel {
            get { return (LexPresetsViewModel) this.DataContext; }
        }
    }
}