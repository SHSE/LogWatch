using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LogWatch.Formats;

namespace LogWatch.Features.SelectSource {
    public class SelectFormatViewModel : ViewModelBase {
        private ILogFormat format;
        private bool? isFormatSelected;

        public SelectFormatViewModel() {
            this.Formats = new ObservableCollection<Lazy<ILogFormat, ILogFormatMetadata>>();

            this.SelectFormatCommand = new RelayCommand<string>(name => {
                this.Format =
                    this.Formats
                        .Where(x => x.Metadata.Name == name)
                        .Select(x => x.Value)
                        .FirstOrDefault();

                this.IsFormatSelected = true;
            });
        }

        public RelayCommand<string> SelectFormatCommand { get; set; }

        public ILogFormat Format {
            get { return this.format; }
            set { this.Set(ref this.format, value); }
        }

        public ObservableCollection<Lazy<ILogFormat, ILogFormatMetadata>> Formats { get; private set; }

        public bool? IsFormatSelected {
            get { return this.isFormatSelected; }
            set { this.Set(ref this.isFormatSelected, value); }
        }

        private void Set<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null) {
            this.Set(propertyName, ref field, newValue, false);
        }
    }
}