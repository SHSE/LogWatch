using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LogWatch.Messages;

namespace LogWatch.Features.RecordDetails {
    public class RecordDetailsViewModel : ViewModelBase {
        private Record record;

        public RecordDetailsViewModel() {
            if (this.IsInDesignMode)
                return;

            this.CopyAllCommand = new RelayCommand(this.CopyAll);
            this.OpenFileCommand = new RelayCommand<string>(url => {
                try {
                    Process.Start(url);
                } catch (FileNotFoundException) {
                    ErrorDialog("File not found: " + url);
                } catch (Win32Exception exception) {
                    ErrorDialog(exception.Message);
                }
            });

            this.MessengerInstance.Register<RecordSelectedMessage>(this, message => { this.Record = message.Record; });
        }

        public Action<string> ErrorDialog { get; set; }

        public Record Record {
            get { return this.record; }
            set { this.Set(ref this.record, value); }
        }

        public RelayCommand CopyAllCommand { get; set; }
        public RelayCommand<string> OpenFileCommand { get; set; }

        private void Set<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null) {
            this.Set(propertyName, ref field, newValue, false);
        }

        private void CopyAll() {
            Clipboard.SetText(string.Concat(this.Record.Message, Environment.NewLine, this.Record.Exception));
        }
    }
}