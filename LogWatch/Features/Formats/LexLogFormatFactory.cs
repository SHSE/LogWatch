using System;
using System.IO;
using System.Linq;

namespace LogWatch.Features.Formats {
    [LogFormatFactory("Lex Scanner")]
    public class LexLogFormatFactory : ILogFormatFactory {
        private const string Prefix = "LogWatch-LexPreset.";

        public ILogFormat Create(Stream stream) {
            var view = new LexSelectView();
            var viewModel = view.ViewModel;

            var files = Directory.EnumerateFiles(".", string.Format("{0}*.dll", Prefix));

            var presets = files
                .Select(filePath => new LexPreset {
                    Name = Uri.UnescapeDataString(filePath.Substring(0, filePath.Length - 4).Substring(Prefix.Length)),
                    FilePath = filePath
                })
                .ToArray();
            
            var compiler = new LexCompiler();

            if (presets.Length == 0) {
                var editView = new LexEditView();
                var editViewModel = editView.ViewModel;

                editViewModel.LogStream = stream;

                if (editView.ShowDialog() != true || editViewModel.IsCompiled == false)
                    return null;

                //if (!editViewModel.Save || string.IsNullOrEmpty(editViewModel.Name))
                return editViewModel.Format;
                
                //var name = Prefix + Uri.EscapeDataString(editViewModel.Name) + ".dll";
                //compiler.Compile(editViewModel.SegmentCode.Text, editViewModel.RecordCode.Text, name);
            }

            viewModel.SelectedPreset = viewModel.Presets.FirstOrDefault();

            if (view.ShowDialog() != true || viewModel.SelectedPreset == null)
                return null;

            var scanners = compiler.LoadCompiled(viewModel.SelectedPreset.FilePath);

            return new LexLogFormat {
                SegmentsScannerType = scanners.SegmentsScannerType,
                RecordsScannerType = scanners.RecordsScannerType
            };
        }

        public bool CanRead(Stream stream) {
            return true;
        }
    }
}