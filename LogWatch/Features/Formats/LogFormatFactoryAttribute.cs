using System;
using System.ComponentModel.Composition;

namespace LogWatch.Features.Formats {
    [MetadataAttribute]
    [AttributeUsage(
        AttributeTargets.Class |
        AttributeTargets.Method |
        AttributeTargets.Property,
        AllowMultiple = false)]
    public sealed class LogFormatFactoryAttribute : ExportAttribute, ILogFormatMetadata {
        public LogFormatFactoryAttribute(string name) : base(typeof (ILogFormatFactory)) {
            this.Name = name;
        }

        public string Name { get; private set; }
    }
}