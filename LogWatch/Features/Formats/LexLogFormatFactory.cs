using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
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

            viewModel.EditPreset = preset => {
                InvalidateCache(preset);
                ShowEditPresetDialog(selectView, stream, preset, presets.Select(x => x.Name));
            };

            viewModel.CreateNewPreset = () => {
                var preset = new LexPreset {
                    Name = "New Preset",
                    SegmentCode = "%%",
                    RecordCode = "%%"
                };

                return ShowEditPresetDialog(selectView, stream, preset, presets.Select(x => x.Name)) ? preset : null;
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

            return Compile(selectedPreset) ?? CompileManually(selectView, stream, selectedPreset);
        }

        public bool CanRead(Stream stream) {
            return true;
        }

        private static void InvalidateCache(LexPreset preset) {
            var directory = GetCacheDirectory();
            var presetAssembly = GetCachedAssembly(preset, directory);

            if (presetAssembly.Exists)
                presetAssembly.Delete();
        }

        private static ILogFormat Compile(LexPreset preset) {
            var compiler = new LexCompiler();

            var directory = GetCacheDirectory();

            LexCompiler.LexFormatScanners result;

            var presetAssembly = GetCachedAssembly(preset, directory);

            if (directory.Exists) {
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

            using (var fileStream = presetAssembly.Create())
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

        private static FileInfo GetCachedAssembly(LexPreset preset, DirectoryInfo directory) {
            return new FileInfo(Path.Combine(directory.FullName, Uri.EscapeDataString(preset.Name) + ".lexpreset"));
        }

        private static DirectoryInfo GetCacheDirectory() {
            return new DirectoryInfo(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LogWatch"));
        }

        private static ILogFormat CompileManually(Window owner, Stream stream, LexPreset preset) {
            var view = new LexPresetView {Owner = owner};
            var viewModel = view.ViewModel;

            viewModel.LogStream = stream;
            viewModel.Name = preset.Name;
            viewModel.CommonCode.Text = preset.CommonCode ?? string.Empty;
            viewModel.SegmentCode.Text = preset.SegmentCode ?? string.Empty;
            viewModel.RecordCode.Text = preset.RecordCode ?? string.Empty;
            viewModel.IsChanged = false;

            if (view.ShowDialog() != true)
                return null;

            return viewModel.Format;
        }

        private static bool ShowEditPresetDialog(Window owner, Stream stream, LexPreset preset,
            IEnumerable<string> names) {
            var view = new LexPresetView {Owner = owner};
            var viewModel = view.ViewModel;

            viewModel.Names = names.ToArray();
            viewModel.LogStream = stream;
            viewModel.Name = preset.Name;
            viewModel.CommonCode.Text = preset.CommonCode ?? string.Empty;
            viewModel.SegmentCode.Text = preset.SegmentCode ?? string.Empty;
            viewModel.RecordCode.Text = preset.RecordCode ?? string.Empty;
            viewModel.IsChanged = false;

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