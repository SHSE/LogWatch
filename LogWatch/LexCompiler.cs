using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using LogWatch.Features.Formats;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace LogWatch {
    public class LexCompiler {
        private const string SegmentsScannerTypeName = "LogWatch.Features.Formats.Lex.Segments.Scanner";
        private const string RecordsScannerTypeName = "LogWatch.Features.Formats.Lex.Records.Scanner";

        public LexCompiler() {
            this.Diagnostics = TextWriter.Null;
        }

        public TextWriter Diagnostics { get; set; }

        private static string Trim(string code) {
            var lines = code.Split(new[] {Environment.NewLine}, StringSplitOptions.None).Select(x => x.Trim());
            return string.Join(Environment.NewLine, lines);
        }

        public LexFormatScanners Compile(
            string segmentScannerCode,
            string recordScannerCode,
            string saveAssemblyTo = null) {
            segmentScannerCode = Trim(segmentScannerCode);
            recordScannerCode = Trim(recordScannerCode);

            this.Diagnostics.WriteLine("Compiling segments scanner");

            var name = string.Format("LexFileFormat-{0}", Guid.NewGuid().ToString("N"));

            var segmentsSyntaxTree = this.CreateSyntaxTree(
                "%using " + typeof (ScanBase).Namespace + ";\n" +
                "%namespace LogWatch.Features.Formats.Lex.Segments\n" +
                "%{\r\npublic override void Begin() { BEGIN(INITIAL); }\r\n%}\n" +
                "%{\r\npublic override string Text { get { return this.yytext; } }\r\n%}\n" +
                "%{\r\npublic void Segment() { this.NextOffset((long) yypos, yyleng); }\r\n%}\n" +
                "%{\r\npublic override System.IO.Stream Source { set { this.SetSource(value); } }\r\n%}\n" +
                segmentScannerCode);

            if (segmentsSyntaxTree == null)
                return null;

            this.Diagnostics.WriteLine("Compiling records scanner");

            var recordsSyntaxTree = this.CreateSyntaxTree(
                "%using " + typeof (ScanBase).Namespace + ";\n" +
                "%namespace LogWatch.Features.Formats.Lex.Records\n" +
                "%{\r\npublic override void Begin() { BEGIN(INITIAL); }\r\n%}\n" +
                "%{\r\npublic override string Text { get { return this.yytext; } }\r\n%}\n" +
                "%{\r\npublic override System.IO.Stream Source { set { this.SetSource(value); } }\r\n%}\n" +
                recordScannerCode);

            if (recordsSyntaxTree == null)
                return null;

            this.Diagnostics.WriteLine("Creating assembly");

            var compilation = this.CreateCompilation(name, segmentsSyntaxTree, recordsSyntaxTree);

            var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName(name), AssemblyBuilderAccess.RunAndCollect);

            var module = assembly.DefineDynamicModule(name);

            var segmentsResult = compilation.Emit(module);

            foreach (var diagnostic in segmentsResult.Diagnostics)
                this.Diagnostics.WriteLine(diagnostic);

            if (saveAssemblyTo != null)
                assembly.Save(saveAssemblyTo);

            return new LexFormatScanners {
                SegmentsScannerType = assembly.GetType(SegmentsScannerTypeName),
                RecordsScannerType = assembly.GetType(RecordsScannerTypeName)
            };
        }

        public LexFormatScanners LoadCompiled(string assemblyFilePath) {
            var assembly = Assembly.LoadFile(assemblyFilePath);

            return new LexFormatScanners {
                SegmentsScannerType = assembly.GetType(SegmentsScannerTypeName),
                RecordsScannerType = assembly.GetType(RecordsScannerTypeName)
            };
        }

        private string CompileLex(string code) {
            var lexFile = Path.ChangeExtension(Path.GetTempFileName(), ".lex");
            var codeFile = Path.ChangeExtension(lexFile, ".cs");

            File.WriteAllText(lexFile, code, Encoding.UTF8);

            var process = Process.Start(new ProcessStartInfo {
                FileName = "gplex.exe",
                Arguments = string.Format("/verbose /version /noPersistBuffer /unicode /out:{0} {1}", codeFile, lexFile),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            process.WaitForExit();

            this.Diagnostics.Write(process.StandardOutput.ReadToEnd());
            this.Diagnostics.Write(process.StandardError.ReadToEnd());

            if (!File.Exists(codeFile))
                return null;

            return File.ReadAllText(codeFile, Encoding.UTF8);
        }

        private Compilation CreateCompilation(string name, params SyntaxTree[] syntaxTrees) {
            var mscorlib = MetadataReference.CreateAssemblyReference("mscorlib");
            var app = new MetadataFileReference(typeof (LexLogFormat).Assembly.Location);

            var compilation = Compilation.Create(
                name,
                syntaxTrees: syntaxTrees,
                references: new[] {mscorlib, app},
                options: new CompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimize: true));

            return compilation;
        }

        private SyntaxTree CreateSyntaxTree(string lexCode) {
            var code = this.CompileLex(lexCode);

            if (code == null)
                return null;

            return SyntaxTree.ParseText(code);
        }

        public class LexFormatScanners {
            public Type SegmentsScannerType { get; set; }
            public Type RecordsScannerType { get; set; }
        }
    }
}