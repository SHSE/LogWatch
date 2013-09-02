using System.Windows;
using Microsoft.Win32;

namespace LogWatch.Features.Formats {
    public partial class LexPresetsView {
        public LexPresetsView() {
            this.InitializeComponent();

            this.Buttons = new[] {this.OkButton, this.CancelButton};
            this.OkButton.Content = "select";

            this.ViewModel.SelectFileForImport = () => {
                var dialog = new OpenFileDialog {
                    Filter = "LogWatch Lex Presets|*.lwlex|All Files|*.*",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog() == true)
                    return dialog.FileName;

                return null;
            };

            this.ViewModel.SelectFileForExport = () => {
                var dialog = new SaveFileDialog {
                    Filter = "LogWatch Lex Presets|*.lwlex|All Files|*.*",
                    OverwritePrompt = true
                };

                if (dialog.ShowDialog() == true)
                    return dialog.FileName;

                return null;
            };

            this.ViewModel.ConfirmDelete = () => ShowMessage("Are you sure?", "Delete", MessageBoxButton.YesNo) == true;
        }

        public LexPresetsViewModel ViewModel {
            get { return (LexPresetsViewModel) this.DataContext; }
        }
    }
}