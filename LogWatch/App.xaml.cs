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
using LogWatch.Features.Formats;
using LogWatch.Features.Sources;

namespace LogWatch {
    public sealed partial class App {
        public static readonly Action<Exception> HandleException = OnException;

        private static CompositionContainer container;

        public static readonly Func<Stream, ILogFormat> SelectFormat =
            stream => {
                if (!Current.CheckAccess())
                    return Current.Dispatcher.InvokeAsync(() => SelectFormat(stream)).Result;

                var formatSelector = new AutoLogFormatSelector();

                container.SatisfyImportsOnce(formatSelector);

                stream.Position = 0;

                var logFormats = formatSelector.SelectFormat(stream).ToArray();

                if (logFormats.Length == 0)
                    return new PlainTextLogFormat();

                if (logFormats.Length == 1)
                    return logFormats[0].Value.Create(stream);

                var view = new SelectFormatView();
                var viewModel = view.ViewModel;

                viewModel.LogStream = stream;

                foreach (var format in logFormats)
                    viewModel.Formats.Add(format);

                viewModel.Formats.Add(
                    new Lazy<ILogFormatFactory, ILogFormatMetadata>(
                        () => new SimpleLogFormatFactory<PlainTextLogFormat>(),
                        new LogFormatFactoryAttribute("Plain text")));

                return view.ShowDialog() != true ? null : viewModel.Format;
            };

        public static LogSourceInfo SourceInfo { get; set; }

        [DebuggerStepThrough]
        private static void OnException(Exception exception) {
            if (Current.Dispatcher.CheckAccess())
                if (exception is AggregateException)
                    ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                else
                    ExceptionDispatchInfo.Capture(exception).Throw();
            else
                Current.Dispatcher.Invoke(() => OnException(exception));
        }

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            TaskScheduler.UnobservedTaskException += (sender, args) => HandleException(args.Exception);

            container = new CompositionContainer(new AssemblyCatalog(typeof (App).Assembly));

            var activationArguments = AppDomain.CurrentDomain.SetupInformation.ActivationArguments;
            var activationData = activationArguments != null ? activationArguments.ActivationData : null;

            var filePath = (activationData ?? e.Args)
                .Select(x => x.Replace("file:///", string.Empty))
                .FirstOrDefault();

            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            if (string.IsNullOrEmpty(filePath)) {
                var view = new SelectSourceView();

                container.SatisfyImportsOnce(view.ViewModel);

                if (view.ShowDialog() == true)
                    SourceInfo = view.ViewModel.Source;
            } else {
                var factory = new FileLogSourceFactory();
                SourceInfo = factory.Create(SelectFormat, filePath);
            }

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
            DialogService.ErrorDialog(e.Exception.ToString());

            if (e.Exception is ApplicationException)
                e.Handled = true;
        }
    }
}