using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using FirstFloor.ModernUI.Windows.Controls;
using LogWatch.Features.SelectSource;
using LogWatch.Formats;
using LogWatch.Sources;
using Microsoft.Win32;

namespace LogWatch {
    public sealed partial class App {
        public static readonly Action<Exception> HandleException =
            exception => {
                if (Current.Dispatcher.CheckAccess())
                    if (exception is AggregateException)
                        ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                    else
                        ExceptionDispatchInfo.Capture(exception).Throw();
                else
                    Current.Dispatcher.Invoke(() => HandleException(exception));
            };

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
            message => ModernDialog.ShowMessage(message, "Log Watch", MessageBoxButton.OK);

        private static readonly Func<string, bool> CollectStatsOnDemand =
            filePath => new FileInfo(filePath).Length >= 10*1024*1024;

        private static readonly CompositionContainer Container = new CompositionContainer(
            new AssemblyCatalog(typeof (App).Assembly));

        public static readonly Func<Stream, ILogFormat> SelectFormat = stream => {
            var formatSelector = new AutoLogFormatSelector();

            Container.SatisfyImportsOnce(formatSelector);

            var logFormats = formatSelector.SelectFormat(stream).ToArray();

            if (logFormats.Length == 0)
                return new PlainTextLogFormat();

            if (logFormats.Length == 1)
                return logFormats[0].Value;

            var view = new SelectFormatView();
            var viewModel = view.ViewModel;

            foreach (var format in logFormats)
                viewModel.Formats.Add(format);

            viewModel.Formats.Add(
                new Lazy<ILogFormat, ILogFormatMetadata>(
                () => new PlainTextLogFormat(),
                new LogFormatAttribute("Plain text")));

            return view.ShowDialog() != true ? null : viewModel.Format;
        };

        public static readonly Func<string, LogSourceInfo> CreateFileLogSource = filePath => {
            ILogFormat logFormat;

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                logFormat = SelectFormat(stream);

            if (logFormat == null)
                return null;

            return new LogSourceInfo(
                new FileLogSource(filePath, logFormat),
                filePath,
                false,
                CollectStatsOnDemand(filePath));
        };

        public static LogSourceInfo SourceInfo { get; set; }

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            TaskScheduler.UnobservedTaskException += (sender, args) => HandleException(args.Exception);

            var activationArguments = AppDomain.CurrentDomain.SetupInformation.ActivationArguments;
            var activationData = activationArguments != null ? activationArguments.ActivationData : null;

            var filePath = (activationData ?? e.Args)
                .Select(x => x.Replace("file:///", string.Empty))
                .FirstOrDefault();

            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            if (string.IsNullOrEmpty(filePath)) {
                var view = new SelectSourceView();

                if (view.ShowDialog() == true)
                    SourceInfo = view.ViewModel.Source;
            } else
                SourceInfo = CreateFileLogSource(filePath);

            if (SourceInfo == null) {
                this.Shutdown();
                return;
            }

            this.ShutdownMode = ShutdownMode.OnLastWindowClose;

            this.MainWindow = new ShellView();
            this.MainWindow.Closed += (sender, args) => SourceInfo.Source.Dispose();
            this.MainWindow.Show();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
            ErrorDialog(e.Exception.ToString());

            if (e.Exception is ApplicationException)
                e.Handled = true;
        }
    }
}