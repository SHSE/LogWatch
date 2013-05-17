using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LogWatch.Annotations;

namespace LogWatch.Features.Formats {
    public class LexPresetsViewModel : ViewModelBase {
        private LexPreset selectedPreset;

        public LexPresetsViewModel() {
            this.Presets = new ObservableCollection<LexPreset>();

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
                    if (this.ConfirmDelete())
                        this.Presets.Remove(this.selectedPreset);
                },
                () => this.selectedPreset != null);

            if (this.IsInDesignMode) {
                this.Presets.Add(new LexPreset {Name = "New Preset"});
                return;
            }
        }

        public Func<bool> ConfirmDelete { get; set; }
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
            }
        }

        public RelayCommand NewCommand { get; set; }
        public RelayCommand EditCommand { get; set; }
        public RelayCommand DuplicateCommand { get; set; }
        public RelayCommand DeleteCommand { get; set; }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            base.RaisePropertyChanged(propertyName);
        }
    }
}