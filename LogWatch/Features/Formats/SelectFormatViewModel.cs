using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;

namespace LogWatch.Features.Formats {
    public class SelectFormatViewModel : ViewModelBase {
        private ILogFormat format;
        private bool? isFormatSelected;

        public SelectFormatViewModel() {
            this.Formats = new ObservableCollection<Lazy<ILogFormatFactory, ILogFormatMetadata>>();

            this.SelectFormatCommand = new RelayCommand<string>(name => {
                var factory = this.Formats
                                  .Where(x => x.Metadata.Name == name)
                                  .Select(x => x.Value)
                                  .First();

                this.Format = factory.Create(this.LogStream);

                if (this.format != null)
                    this.IsFormatSelected = true;
            });
        }

        public Stream LogStream { get; set; }

        public RelayCommand<string> SelectFormatCommand { get; set; }

        public ILogFormat Format {
            get { return this.format; }
            set { this.Set(ref this.format, value); }
        }

        public ObservableCollection<Lazy<ILogFormatFactory, ILogFormatMetadata>> Formats { get; private set; }

        public bool? IsFormatSelected {
            get { return this.isFormatSelected; }
            set { this.Set(ref this.isFormatSelected, value); }
        }

        private void Set<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null) {
            this.Set(propertyName, ref field, newValue, false);
        }
    }
}