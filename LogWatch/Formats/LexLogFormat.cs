using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace LogWatch.Formats {
    public class LexLogFormat : ILogFormat {
        private const string ScannerBase = @"
            using System;
            using System.Collections.Generic;
            
            namespace LogWatch.Formats.Lex {
                public abstract class ScanBase {
                    public Action<long, int> OffsetCallback;

                    protected void NextOffset(long offset, int length) {
                        OffsetCallback(offset, length);
                    }

                    public abstract int yylex();

                    protected virtual bool yywrap() {
                        return true;
                    }

                    public string Timestamp { get; set; }            
                    public string Level { get; set; }
                    public string Logger { get; set; }
                    public string Message { get; set; }
                    public string Exception { get; set; }
                }

                public enum Tokens {
                    EOF = 0,
                    maxParseToken = int.MaxValue
                }
            }
        ";

        private const string LexSegmentsFooter = @"
            %namespace LogWatch.Formats.Lex

            %%

            {record} this.NextOffset((long) yypos, yyleng);";
        private readonly ThreadLocal<dynamic> reocordsScanner;

        private Type recordsScannerType;
        private Type segmentsScannerType;

        public LexLogFormat() {
            this.reocordsScanner = new ThreadLocal<dynamic>(() => Activator.CreateInstance(this.recordsScannerType));
        }

        public string SegmentsExpression { get; set; }
        public string RecordsExpression { get; set; }

        public TextWriter Diagnostics { get; set; }

        public Record DeserializeRecord(ArraySegment<byte> segment) {
            var scanner = this.reocordsScanner.Value;

            scanner.SetSource((Stream) new MemoryStream(segment.Array, segment.Offset, segment.Count));
            scanner.yylex();

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
                dynamic scanner = Activator.CreateInstance(this.segmentsScannerType);

                scanner.OffsetCallback =
                    new Action<long, int>((offset, length) => observer.OnNext(new RecordSegment(offset, length)));

                scanner.SetSource(stream);
                scanner.yylex();

                return -1L;
            });
        }

        public bool CanRead(Stream stream) {
            return true;
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

            var code = File.ReadAllText(codeFile, Encoding.UTF8);

            var codeTree = SyntaxTree.ParseText(code);
            var scannerTree = SyntaxTree.ParseText(ScannerBase);

            foreach (var diagnostic in codeTree.GetDiagnostics())
                diagnostics.WriteLine(diagnostic);

            var mscorlib = MetadataReference.CreateAssemblyReference("mscorlib");

            var name = string.Format("LexFileFormat-{0}", Guid.NewGuid().ToString("N"));

            var compilation = Compilation.Create(
                outputName: name,
                syntaxTrees: new[] {codeTree, scannerTree},
                references: new[] {mscorlib},
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
            var lexCode = string.Join(
                Environment.NewLine,
                "%namespace LogWatch.Formats.Lex",
                this.RecordsExpression);

            this.recordsScannerType = Compile(lexCode, this.Diagnostics, "LogWatch.Formats.Lex.Scanner");

            return this.recordsScannerType != null;
        }

        public bool TryCompileSegmentsScanner() {
            if (Regex.IsMatch(this.SegmentsExpression,@"^record\s+\S+")) {
                this.Diagnostics.WriteLine("Expected 'record' token definition");
                return false;
            }

            var lexCode = string.Join(
                Environment.NewLine,
                new[] {this.SegmentsExpression}
                    .Concat(LexSegmentsFooter
                        .Split(new[] {Environment.NewLine}, StringSplitOptions.None)
                        .Select(x => x.Trim())));

            this.segmentsScannerType = Compile(lexCode, this.Diagnostics, "LogWatch.Formats.Lex.Scanner");

            return this.segmentsScannerType != null;
        }
    }
}