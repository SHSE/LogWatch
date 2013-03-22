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

        public string Test {
            get {
                return
                    @"System.ArgumentException: Destination array is not long enough to copy all the items in the collection. Check array index and length." +
                    Environment.NewLine +
                    @"   at System.ThrowHelper.ThrowArgumentException(ExceptionResource resource)" + Environment.NewLine +
                    @"   at System.BitConverter.ToUInt16(Byte[] value, Int32 startIndex)" + Environment.NewLine +
                    @"   at SharpCompress.Common.Zip.Headers.ZipFileEntry.LoadExtra(Byte[] extra)" + Environment.NewLine +
                    @"   at SharpCompress.Common.Zip.Headers.LocalEntryHeader.Read(BinaryReader reader)" +
                    Environment.NewLine +
                    @"   at SharpCompress.Common.Zip.ZipHeaderFactory.ReadHeader(UInt32 headerBytes, BinaryReader reader)" +
                    Environment.NewLine +
                    @"   at SharpCompress.Common.Zip.SeekableZipHeaderFactory.GetLocalHeader(Stream stream, DirectoryEntryHeader directoryEntryHeader)" +
                    Environment.NewLine +
                    @"   at SharpCompress.Common.Zip.SeekableZipFilePart.LoadLocalHeader()" + Environment.NewLine +
                    @"   at SharpCompress.Common.Zip.SeekableZipFilePart.GetStream()" + Environment.NewLine +
                    @"   at SharpCompress.Archive.Zip.ZipArchiveEntry.OpenEntryStream()" + Environment.NewLine +
                    @"   at SharpCompress.Utility.Extract[TEntry,TVolume](TEntry entry, AbstractArchive`2 archive, Stream streamToWriteTo)" +
                    Environment.NewLine +
                    @"   at SharpCompress.Archive.Zip.ZipArchiveEntry.WriteTo(Stream streamToWriteTo)" +
                    Environment.NewLine +
                    @"   at Forensics.Worker.Indexers.ArchiveIndexer.ArchiveEntryInfo.get_FileStream() in x:\TeamCity\buildAgent\work\8712fdd031dc966\Forensics.Worker\Indexers\ArchiveIndexer.cs:line 131" +
                    Environment.NewLine +
                    @"   at Forensics.Worker.FileIndexContext.get_FileStream() in x:\TeamCity\buildAgent\work\8712fdd031dc966\Forensics.Worker\FileIndexContext.cs:line 93" +
                    Environment.NewLine +
                    @"   at Forensics.Worker.Impl.WorkerTaskExecutorDomainProxy.<.ctor>b__4(FileIndexContext context) in x:\TeamCity\buildAgent\work\8712fdd031dc966\Forensics.Worker\Impl\WorkerTaskExecutorDomainProxy.cs:line 69" +
                    Environment.NewLine +
                    @"   at Forensics.Worker.Impl.WorkerTaskExecutor.OnInitializingContext(FileIndexContext context) in x:\TeamCity\buildAgent\work\8712fdd031dc966\Forensics.Worker\Impl\WorkerTaskExecutor.cs:line 161" +
                    Environment.NewLine +
                    @"   at Forensics.Worker.Impl.WorkerTaskExecutor.IndexFile(FileIndexContext context, IDictionary`2 footprints) in x:\TeamCity\buildAgent\work\8712fdd031dc966\Forensics.Worker\Impl\WorkerTaskExecutor.cs:line 176" +
                    Environment.NewLine +
                    @"   at Forensics.Worker.Impl.WorkerTaskExecutor.<>c__DisplayClass13.<ExecuteTask>b__d(FileIndexContext subcontext) in x:\TeamCity\buildAgent\work\8712fdd031dc966\Forensics.Worker\Impl\WorkerTaskExecutor.cs:line 295" +
                    Environment.NewLine +
                    @"   at Forensics.Worker.FileIndexContext.IndexSubfile(IFileInfo subfileInfo, IFileContext subfileContext, String relativeParentId) in x:\TeamCity\buildAgent\work\8712fdd031dc966\Forensics.Worker\FileIndexContext.cs:line 147" +
                    Environment.NewLine +
                    @"   at Forensics.Worker.Indexers.ArchiveIndexer.IndexArchive(IFileIndexContext context, IArchive archive) in x:\TeamCity\buildAgent\work\8712fdd031dc966\Forensics.Worker\Indexers\ArchiveIndexer.cs:line 98" +
                    Environment.NewLine +
                    @"   at Forensics.Worker.Indexers.ArchiveIndexer.Index(IFileIndexContext context) in x:\TeamCity\buildAgent\work\8712fdd031dc966\Forensics.Worker\Indexers\ArchiveIndexer.cs:line 67" +
                    Environment.NewLine +
                    @"   at Forensics.Worker.Impl.WorkerTaskExecutor.IndexFile(FileIndexContext context, IDictionary`2 footprints) in x:\TeamCity\buildAgent\work\8712fdd031dc966\Forensics.Worker\Impl\WorkerTaskExecutor.cs:line 198" +
                    Environment.NewLine;
            }
        }

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