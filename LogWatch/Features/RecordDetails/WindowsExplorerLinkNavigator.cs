using System;
using System.Diagnostics;
using System.Windows;
using FirstFloor.ModernUI.Windows;
using FirstFloor.ModernUI.Windows.Navigation;

namespace LogWatch.Features.RecordDetails {
    public class WindowsExplorerLinkNavigator : ILinkNavigator {
        public WindowsExplorerLinkNavigator() {
            this.Commands = new CommandDictionary();
        }

        public void Navigate(Uri uri, FrameworkElement source, string parameter = null) {
            Process.Start(uri.LocalPath, parameter);
        }

        public CommandDictionary Commands { get; set; }
    }
}