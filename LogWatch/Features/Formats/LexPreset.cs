using System.ComponentModel;
using System.Runtime.CompilerServices;
using LogWatch.Annotations;

namespace LogWatch.Features.Formats {
    public class LexPreset : INotifyPropertyChanged {
        private string commonCode;
        private string name;
        private string recordCode;
        private string segmentCode;

        public string Name {
            get { return this.name; }
            set {
                if (value == this.name)
                    return;
                this.name = value;
                this.OnPropertyChanged();
            }
        }

        public string CommonCode {
            get { return this.commonCode; }
            set {
                if (value == this.commonCode)
                    return;
                this.commonCode = value;
                this.OnPropertyChanged();
            }
        }

        public string SegmentCode {
            get { return this.segmentCode; }
            set {
                if (value == this.segmentCode)
                    return;
                this.segmentCode = value;
                this.OnPropertyChanged();
            }
        }

        public string RecordCode {
            get { return this.recordCode; }
            set {
                if (value == this.recordCode)
                    return;
                this.recordCode = value;
                this.OnPropertyChanged();
            }
        }

        public ILogFormat Format { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            var handler = this.PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}