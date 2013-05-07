using System.IO;

namespace LogWatch.Features.Formats {
    [LogFormatFactory("Lex Scanner")]
    public class LexLogFormatFactory : ILogFormatFactory {
        public ILogFormat Create(Stream stream) {
            var view = new LexView();
            var viewModel = view.ViewModel;

            viewModel.LogStream = stream;

            if (view.ShowDialog() != true)
                return null;

            return viewModel.Format;
        }

        public bool CanRead(Stream stream) {
            return true;
        }
    }
}