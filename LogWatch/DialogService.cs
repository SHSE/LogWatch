using System;
using System.Windows;
using FirstFloor.ModernUI.Windows.Controls;
using Microsoft.Win32;

namespace LogWatch {
    internal static class DialogService {
        public static readonly Func<string> OpenFileDialog =
            () => {
                var dialog = new OpenFileDialog {
                    CheckFileExists = true,
                    CheckPathExists = true
                };

                if (dialog.ShowDialog() == true)
                    return dialog.FileName;

                return null;
            };

        public static readonly Action<string> ErrorDialog =
            message => ModernDialog.ShowMessage(message, "Error", MessageBoxButton.OK);

        public static readonly Action<string> InfoDialog =
            message => ModernDialog.ShowMessage(message, "Info", MessageBoxButton.OK);
    }
}