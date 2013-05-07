using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace LogWatch.Features.Formats {
    public class LexLogFormat : ILogFormat {
        private ThreadLocal<IScanner> reocordsScanner;

        private Type recordsScannerType;
        private Type segmentsScannerType;

        public LexLogFormat() {
            
            this.Diagnostics = new StringWriter();
        }

        public string SegmentCode { get; set; }
        public string RecordCode { get; set; }

        public TextWriter Diagnostics { get; set; }

        public Record DeserializeRecord(ArraySegment<byte> segment) {
            var scanner = this.reocordsScanner.Value;

            scanner.Begin();
            scanner.Reset();
            scanner.Source = new MemoryStream(segment.Array, segment.Offset, segment.Count);
            scanner.Parse();

            return new Record {
                Level = GetLevel(scanner.Level),
                Logger = scanner.Logger,
                Message = scanner.Message,
                Exception = scanner.Exception
            };
        }

        public Task<long> ReadSegments(
            IObserver<RecordSegment> observer,
            Stream stream,
            CancellationToken cancellationToken) {
            return Task.Factory.StartNew(() => {
                var scanner = (IScanner) Activator.CreateInstance(this.segmentsScannerType);

                scanner.OffsetCallback = (offset, length) => observer.OnNext(new RecordSegment(offset, length));

                scanner.Source = stream;
                scanner.Parse();

                return -1L;
            });
        }

        private static LogLevel? GetLevel(string levelString) {
            if (levelString == null)
                return null;

            switch (levelString.ToLower()) {
                case "trace":
                    return LogLevel.Trace;
                case "debug":
                    return LogLevel.Debug;
                case "info":
                    return LogLevel.Info;
                case "warn":
                    return LogLevel.Warn;
                case "error":
                    return LogLevel.Error;
                case "fatal":
                    return LogLevel.Fatal;
                default:
                    return null;
            }
        }

        private static Type Compile(string lexCode, TextWriter diagnostics, string typeName) {
            var lexFile = Path.ChangeExtension(Path.GetTempFileName(), ".lex");
            var codeFile = Path.ChangeExtension(lexFile, ".cs");

            File.WriteAllText(lexFile, lexCode, Encoding.UTF8);

            var process = Process.Start(new ProcessStartInfo {
                FileName = "gplex.exe",
                Arguments = string.Format("/verbose /version /noPersistBuffer /unicode /out:{0} {1}", codeFile, lexFile),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            process.WaitForExit();

            diagnostics.Write(process.StandardOutput.ReadToEnd());
            diagnostics.Write(process.StandardError.ReadToEnd());

            if (!File.Exists(codeFile))
                return null;

            var code = File.ReadAllText(codeFile, Encoding.UTF8);

            var codeTree = SyntaxTree.ParseText(code);

            foreach (var diagnostic in codeTree.GetDiagnostics())
                diagnostics.WriteLine(diagnostic);

            var mscorlib = MetadataReference.CreateAssemblyReference("mscorlib");
            var app = new MetadataFileReference(typeof (LexLogFormat).Assembly.Location);

            var name = string.Format("LexFileFormat-{0}", Guid.NewGuid().ToString("N"));

            var compilation = Compilation.Create(
                outputName: name,
                syntaxTrees: new[] {codeTree},
                references: new[] {mscorlib, app},
                options: new CompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimize: true));

            var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName(name), AssemblyBuilderAccess.RunAndCollect);
            
            var module = assembly.DefineDynamicModule(name);

            var result = compilation.Emit(module);

            foreach (var diagnostic in result.Diagnostics)
                diagnostics.WriteLine(diagnostic);

            return assembly.GetType(typeName);
        }

        public bool TryCompileRecordsScanner() {
            var lexCode =
                "%namespace LogWatch.Features.Formats.RecordsScanner\n" +
                "%{\r\npublic override void Begin() { BEGIN(INITIAL); }\r\n%}\n" +
                "%{\r\npublic override string Text { get { return this.yytext; } }\r\n%}\n" +
                "%{\r\npublic override System.IO.Stream Source { set { this.SetSource(value); } }\r\n%}\n" +
                this.Trim(this.RecordCode);

            this.recordsScannerType = Compile(lexCode, this.Diagnostics, "LogWatch.Features.Formats.RecordsScanner.Scanner");

            this.reocordsScanner = new ThreadLocal<IScanner>(() => (IScanner) Activator.CreateInstance(this.recordsScannerType));

            return this.recordsScannerType != null;
        }

        public bool TryCompileSegmentsScanner() {
            if (Regex.IsMatch(this.SegmentCode, @"^record\s+\S+")) {
                this.Diagnostics.WriteLine("Expected 'record' token definition");
                return false;
            }

            var lexCode = "%namespace LogWatch.Features.Formats.SegmentsScanner\n" +
                          "%{\r\npublic override void Begin() { BEGIN(INITIAL); }\r\n%}\n" +
                          "%{\r\npublic override string Text { get { return this.yytext; } }\r\n%}\n" +
                          "%{\r\npublic void Segment() { this.NextOffset((long) yypos, yyleng); }\r\n%}\n" +
                          "%{\r\npublic override System.IO.Stream Source { set { this.SetSource(value); } }\r\n%}\n" +
                          this.Trim(this.SegmentCode);

            this.segmentsScannerType = Compile(lexCode, this.Diagnostics, "LogWatch.Features.Formats.SegmentsScanner.Scanner");

            return this.segmentsScannerType != null;
        }

        private string Trim(string code) {
            var lines = code.Split(new[] {Environment.NewLine}, StringSplitOptions.None).Select(x => x.Trim());
            return string.Join(Environment.NewLine, lines);
        }
    }

    public interface IScanner {
        string Timestamp { get; }
        string Level { get; }
        string Logger { get; }
        string Message { get; }
        string Exception { get; }

        Action<long, int> OffsetCallback { set; }
        Stream Source { set; }

        int Parse();
        void Reset();
        void Begin();
    }

    public abstract class ScanBase : IScanner {
        public abstract string Text { get; }
        public Action<long, int> OffsetCallback { get; set; }

        public int Parse() {
            return this.yylex();
        }

        public abstract void Begin();
        public abstract Stream Source { set; }

        public string Timestamp { get; set; }
        public string Level { get; set; }
        public string Logger { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }

        public void Reset() {
            this.Timestamp = null;
            this.Level = null;
            this.Logger = null;
            this.Message = null;
            this.Exception = null;
        }

        protected void NextOffset(long offset, int length) {
            this.OffsetCallback(offset, length);
        }

        public abstract int yylex();

        protected virtual bool yywrap() {
            return true;
        }
    }

    public enum Tokens {
        EOF = 0,
        maxParseToken = int.MaxValue
    }
}