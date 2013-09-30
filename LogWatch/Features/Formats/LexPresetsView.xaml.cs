using LogWatch.Controls;
using Microsoft.Win32;

namespace LogWatch.Features.Formats {
    public partial class LexPresetsView {
        public LexPresetsView() {
            this.InitializeComponent();

            this.ViewModel.SelectionCompleted = () => {
                this.DialogResult = true;
                this.Close();
            };

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

            this.ViewModel.ConfirmDelete = preset => {
                var result = CustomModernDialog.ShowMessage(
                    string.Format("Are you sure you want to delete preset \"{0}\"?", preset.Name),
                    "Lex Presets",
                    this,
                    new ButtonDef("delete", "delete preset"),
                    ButtonDef.Cancel);

                return result == "delete";
            };
        }

        public LexPresetsViewModel ViewModel {
            get { return (LexPresetsViewModel) this.DataContext; }
        }
    }
}