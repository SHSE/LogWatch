using System;
using System.ComponentModel.Composition;

namespace LogWatch.Formats {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false), MetadataAttribute]
    public sealed class LogFormatAttribute : ExportAttribute, ILogFormatMetadata {
        public LogFormatAttribute(string name) : base(typeof (ILogFormat)) {
            this.Name = name;
        }

        public string Name { get; private set; }
    }
}