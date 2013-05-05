using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LogWatch.Features.Formats;

namespace LogWatch.Features.Sources {
    public class SelectSourceViewModel : ViewModelBase {
        private bool? isLogSourceSelected;
        private LogSourceInfo source;
        private IEnumerable<Lazy<ILogSourceFactory, ILogSourceMetadata>> sources;

        public SelectSourceViewModel() {
            this.SelectSourceCommand = new RelayCommand<ILogSourceFactory>(factory => {
                this.Source = factory.Create(this.SelectLogFormat);

                if (this.source != null)
                    this.IsLogSourceSelected = true;
            });

            if (this.IsInDesignMode)
                return;

            this.SelectLogFormat = stream => null;
        }

        [ImportMany(typeof (ILogSourceFactory))]
        public IEnumerable<Lazy<ILogSourceFactory, ILogSourceMetadata>> Sources {
            get { return this.sources; }
            set { this.Set(ref this.sources, value); }
        }

        public Func<Stream, ILogFormat> SelectLogFormat { get; set; }
        public Action<Exception> HandleException { get; set; }

        public bool? IsLogSourceSelected {
            get { return this.isLogSourceSelected; }
            set { this.Set(ref this.isLogSourceSelected, value); }
        }

        public LogSourceInfo Source {
            get { return this.source; }
            set { this.Set(ref this.source, value); }
        }

        public RelayCommand<ILogSourceFactory> SelectSourceCommand { get; set; }

        private void Set<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null) {
            this.Set(propertyName, ref field, newValue, false);
        }
    }
}