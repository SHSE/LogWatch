using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using LogWatch.Properties;

namespace LogWatch.Features.Formats {
    [LogFormatFactory("Lex Scanner")]
    public class LexLogFormatFactory : ILogFormatFactory {
        public ILogFormat Create(Stream stream) {
            var savedPresets = Settings.Default.LexPresets ?? new XElement("Presets");

            var presets = new List<LexPreset>(
                from element in savedPresets.Elements("Preset")
                select new LexPreset {
                    Name = (string) element.Attribute("Name"),
                    CommonCode = (string) element.Attribute("CommonCode"),
                    SegmentCode = (string) element.Attribute("SegmentCode"),
                    RecordCode = (string) element.Attribute("RecordCode")
                });


            var selectView = new LexPresetsView();
            var viewModel = selectView.ViewModel;

            foreach (var preset in presets)
                viewModel.Presets.Add(preset);

            viewModel.SelectedPreset = viewModel.Presets.FirstOrDefault();
            viewModel.EditPreset = preset => ShowEditPresetDialog(stream, preset);
            viewModel.CreateNewPreset = () => {
                var preset = new LexPreset {
                    Name = "New Preset",
                    CommonCode =
                        "timestamp [^;\\r\\n]+\n" +
                        "level     [^;\\r\\n]+\n" +
                        "logger    [^;\\r\\n]+\n" +
                        "message   [^;\\r\\n]+\n" +
                        "exception [^;\\r\\n]*",

                    SegmentCode =
                        "record {timestamp}[;]{message}[;]{logger}[;]{level}[;]{exception}\\r\\n\n" +
                        "%%\n" +
                        "{record} Segment();",

                    RecordCode =
                        "%x MATCHED_TIMESTAMP\n" +
                        "%x MATCHED_MESSAGE\n" +
                        "%x MATCHED_LEVEL\n" +
                        "%x MATCHED_LOGGER\n" +
                        "%%\n" +
                        "<INITIAL>{timestamp} Timestamp = yytext; BEGIN(MATCHED_TIMESTAMP);\n" +
                        "<MATCHED_TIMESTAMP>{message} this.Message = yytext; BEGIN(MATCHED_MESSAGE);\n" +
                        "<MATCHED_MESSAGE>{logger} this.Logger = yytext; BEGIN(MATCHED_LOGGER);\n" +
                        "<MATCHED_LOGGER>{level} this.Level = yytext; BEGIN(MATCHED_LEVEL);\n" +
                        "<MATCHED_LEVEL>{exception} this.Exception = yytext; BEGIN(INITIAL);"
                };

                return ShowEditPresetDialog(stream, preset) ? preset : null;
            };

            if (selectView.ShowDialog() != true)
                return null;

            Settings.Default.LexPresets =
                new XElement("Presets",
                    from preset in viewModel.Presets
                    select new XElement("Preset",
                        new XAttribute("Name", preset.Name),
                        new XAttribute("CommonCode", preset.CommonCode),
                        new XAttribute("SegmentCode", preset.SegmentCode),
                        new XAttribute("RecordCode", preset.RecordCode)));

            Settings.Default.Save();

            var selectedPreset = selectView.ViewModel.SelectedPreset;

            if (selectedPreset == null)
                return null;

            if (selectedPreset.Format != null)
                return selectedPreset.Format;

            return Compile(selectedPreset) ?? CompileManually(stream, selectedPreset);
        }

        public bool CanRead(Stream stream) {
            return true;
        }

        private static ILogFormat Compile(LexPreset preset) {
            var compiler = new LexCompiler();

            var directory = new DirectoryInfo(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LogWatch"));

            LexCompiler.LexFormatScanners result;

            if (directory.Exists) {
                var presetAssembly = new FileInfo(
                    Path.Combine(directory.FullName, Uri.EscapeDataString(preset.Name) + ".lexpreset"));

                if (presetAssembly.Exists) {
                    using (var fileStream = presetAssembly.OpenRead())
                        result = compiler.LoadCompiled(fileStream);

                    if (result.Success)
                        return new LexLogFormat {
                            SegmentsScannerType = result.SegmentsScannerType,
                            RecordsScannerType = result.RecordsScannerType
                        };
                }
            } else
                directory.Create();

            using (var fileStream =
                File.Create(Path.Combine(directory.FullName, Uri.EscapeDataString(preset.Name) + ".lexpreset")))
                result = compiler.Compile(
                    string.Concat(preset.CommonCode, Environment.NewLine, preset.SegmentCode),
                    string.Concat(preset.CommonCode, Environment.NewLine, preset.RecordCode),
                    fileStream);

            if (result.Success)
                return new LexLogFormat {
                    SegmentsScannerType = result.SegmentsScannerType,
                    RecordsScannerType = result.RecordsScannerType
                };

            return null;
        }

        private static ILogFormat CompileManually(Stream stream, LexPreset preset) {
            var view = new LexPresetView();
            var viewModel = view.ViewModel;

            viewModel.LogStream = stream;
            viewModel.Name = preset.Name;
            viewModel.CommonCode.Text = preset.CommonCode ?? string.Empty;
            viewModel.SegmentCode.Text = preset.SegmentCode ?? string.Empty;
            viewModel.RecordCode.Text = preset.RecordCode ?? string.Empty;

            if (view.ShowDialog() != true)
                return null;

            return viewModel.Format;
        }

        private static bool ShowEditPresetDialog(Stream stream, LexPreset preset) {
            var view = new LexPresetView();
            var viewModel = view.ViewModel;

            viewModel.LogStream = stream;
            viewModel.Name = preset.Name;
            viewModel.CommonCode.Text = preset.CommonCode ?? string.Empty;
            viewModel.SegmentCode.Text = preset.SegmentCode ?? string.Empty;
            viewModel.RecordCode.Text = preset.RecordCode ?? string.Empty;

            if (view.ShowDialog() != true)
                return false;

            preset.Name = viewModel.Name;
            preset.CommonCode = viewModel.CommonCode.Text;
            preset.SegmentCode = viewModel.SegmentCode.Text;
            preset.RecordCode = viewModel.RecordCode.Text;
            preset.Format = viewModel.IsCompiled ? viewModel.Format : null;

            return true;
        }
    }
}