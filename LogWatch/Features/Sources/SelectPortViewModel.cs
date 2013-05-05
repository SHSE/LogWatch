using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace LogWatch.Features.Sources {
    public class SelectPortViewModel : ViewModelBase {
        private bool? isPortSet;
        private int? port;

        public SelectPortViewModel() {
            if (this.IsInDesignMode)
                return;

            this.Port = 65000;

            this.SelectPortCommand = new RelayCommand(
                () => this.IsPortSet = true,
                () => this.Port != null && this.Port > 0);
        }

        public int? Port {
            get { return this.port; }
            set { this.Set(ref this.port, value); }
        }

        public bool? IsPortSet {
            get { return this.isPortSet; }
            set { this.Set(ref this.isPortSet, value); }
        }

        public RelayCommand SelectPortCommand { get; set; }

        private void Set<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null) {
            this.Set(propertyName, ref field, newValue, false);
        }
    }
}