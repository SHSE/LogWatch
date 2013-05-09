using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using GalaSoft.MvvmLight;
using LogWatch.Annotations;

namespace LogWatch {
    public class LexSelectViewModel : ViewModelBase {
        private LexPreset selectedPreset;

        public LexSelectViewModel() {
            this.Presets = new ObservableCollection<LexPreset>();
        }

        public ObservableCollection<LexPreset> Presets { get; set; }

        public LexPreset SelectedPreset {
            get { return this.selectedPreset; }
            set {
                this.selectedPreset = value;
                this.OnPropertyChanged();
            }
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged(string propertyName = null) {
            base.RaisePropertyChanged(propertyName);
        }

       

       
    }

     public class LexPreset {
            public string Name { get; set; }
            public string FilePath { get; set; }
        }
}