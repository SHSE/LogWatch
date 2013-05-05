using System;
using System.ComponentModel.Composition;

namespace LogWatch.Features.Sources {
    [MetadataAttribute]
    [AttributeUsage(
        AttributeTargets.Class |
        AttributeTargets.Method |
        AttributeTargets.Property,
        AllowMultiple = false)]
    public sealed class LogSourceFactoryAttribute : ExportAttribute, ILogSourceMetadata {
        public LogSourceFactoryAttribute(string name) : base(typeof (ILogSourceFactory)) {
            this.Name = name;
        }

        public string Name { get; private set; }
    }
}