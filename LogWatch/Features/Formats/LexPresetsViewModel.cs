using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LogWatch.Annotations;

namespace LogWatch.Features.Formats {
    public class LexPresetsViewModel : ViewModelBase {
        private LexPreset selectedPreset;

        public Action SelectionCompleted { get; set; }

        public LexPresetsViewModel() {
            this.Presets = new ObservableCollection<LexPreset>();

            this.SelectCommand = new RelayCommand(
                () => SelectionCompleted(), 
                () => this.SelectedPreset != null);

            this.NewCommand = new RelayCommand(() => {
                var preset = this.CreateNewPreset();

                if (preset != null) {
                    this.Presets.Add(preset);
                    this.SelectedPreset = preset;
                }
            });

            this.EditCommand = new RelayCommand(
                () => this.EditPreset(this.selectedPreset),
                () => this.selectedPreset != null);

            this.DuplicateCommand = new RelayCommand(
                () => {
                    var preset = new LexPreset {
                        Name = this.selectedPreset.Name + " Copy",
                        CommonCode = this.selectedPreset.CommonCode,
                        SegmentCode = this.selectedPreset.SegmentCode,
                        RecordCode = this.selectedPreset.RecordCode
                    };

                    this.Presets.Add(preset);
                    this.SelectedPreset = preset;
                },
                () => this.selectedPreset != null);

            this.DeleteCommand = new RelayCommand(
                () => {
                    if (this.ConfirmDelete(this.selectedPreset))
                        this.Presets.Remove(this.selectedPreset);
                },
                () => this.selectedPreset != null);

            this.ExportCommand = new RelayCommand(
                () => Export(this.SelectedPreset),
                () => this.SelectedPreset != null);
            
            this.ImportCommand = new RelayCommand(this.Import);

            if (this.IsInDesignMode)
                this.Presets.Add(new LexPreset {Name = "New Preset"});
        }

        public Func<string> SelectFileForExport { get; set; }
        public Func<string> SelectFileForImport { get; set; }

        private void Export(LexPreset preset) {
            var fileName = SelectFileForExport();

            if (fileName == null)
                return;

            var document = new XDocument(
                new XElement("Preset",
                    new XAttribute("Name", preset.Name),
                    new XElement("Common", preset.CommonCode),
                    new XElement("Segment", preset.SegmentCode),
                    new XElement("Record", preset.RecordCode)));

            document.Save(fileName);
        }

        private void Import() {
             var fileName = SelectFileForImport();

            if (fileName == null)
                return;

            try {
                var document = XDocument.Load(fileName).Root ?? new XElement("Preset");

                var preset = new LexPreset {
                    Name = (string) document.Attribute("Name"),
                    CommonCode = (string) document.Element("Common"),
                    SegmentCode = (string) document.Element("Segment"),
                    RecordCode = (string) document.Element("Record")
                };

                this.Presets.Add(preset);
            } catch (XmlException exception) {
                throw new ApplicationException("Invalid file format", exception);
            }
        }

        public Func<LexPreset, bool> ConfirmDelete { get; set; }
        public Func<LexPreset> CreateNewPreset { get; set; }
        public Action<LexPreset> EditPreset { get; set; }

        public ObservableCollection<LexPreset> Presets { get; set; }

        public LexPreset SelectedPreset {
            get { return this.selectedPreset; }
            set {
                if (Equals(value, this.selectedPreset))
                    return;
                this.selectedPreset = value;
                this.OnPropertyChanged();
                this.EditCommand.RaiseCanExecuteChanged();
                this.DuplicateCommand.RaiseCanExecuteChanged();
                this.DeleteCommand.RaiseCanExecuteChanged();
                this.ExportCommand.RaiseCanExecuteChanged();
                this.SelectCommand.RaiseCanExecuteChanged();
            }
        }

        public RelayCommand NewCommand { get; set; }
        public RelayCommand EditCommand { get; set; }
        public RelayCommand DuplicateCommand { get; set; }
        public RelayCommand DeleteCommand { get; set; }
        public RelayCommand ExportCommand { get; set; }
        public RelayCommand ImportCommand { get; set; }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            base.RaisePropertyChanged(propertyName);
        }

        public RelayCommand SelectCommand { get; set; }
    }
}