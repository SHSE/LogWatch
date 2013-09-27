using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace LogWatch.Features.Formats {
    public class LexCompiler {
        private const string BufferWithEncodingAndByteCounterCode = @"
        internal class BufferWithEncodingAndByteCounter : ScanBuff {
            private class BufferElement {
                private bool appendToNext;
                private int brkIx;
                private int minIx;
                private StringBuilder bldr = new StringBuilder();
                private StringBuilder next = new StringBuilder();

                internal int MaxIndex { get; private set; }

                internal char this[int index] {
                    get {
                        if (index < this.minIx || index >= this.MaxIndex)
                            throw new BufferException(""Index was outside data buffer"");
                        else if (index < this.brkIx)
                            return this.bldr[index - this.minIx];
                        else
                            return this.next[index - this.brkIx];
                    }
                }

                internal void Append(char[] block, int count) {
                    this.MaxIndex += count;
                    if (this.appendToNext)
                        this.next.Append(block, 0, count);
                    else {
                        this.bldr.Append(block, 0, count);
                        this.brkIx = this.MaxIndex;
                        this.appendToNext = true;
                    }
                }

                internal string GetString(int start, int limit) {
                    if (limit <= start)
                        return """";
                    if (start >= this.minIx && limit <= this.MaxIndex)
                        if (limit < this.brkIx) // String entirely in bldr builder
                            return this.bldr.ToString(start - this.minIx, limit - start);
                        else if (start >= this.brkIx) // String entirely in next builder
                            return this.next.ToString(start - this.brkIx, limit - start);
                        else // Must do a string-concatenation
                            return
                                this.bldr.ToString(start - this.minIx, this.brkIx - start) +
                                this.next.ToString(0, limit - this.brkIx);
                    else
                        throw new BufferException(""String was outside data buffer"");
                }

                internal void Mark(int limit) {
                    if (limit <= this.brkIx + 16)
                        return;

                    var temp = this.bldr;
                    this.bldr = this.next;
                    this.next = temp;
                    this.next.Length = 0;
                    this.minIx = this.brkIx;
                    this.brkIx = this.MaxIndex;
                }
            }

            private readonly BufferElement data = new BufferElement();

            private int bPos; // Postion index in the StringBuilder
            private readonly BlockReader bextBlock; // Delegate that serves char-arrays;

            private string EncodingName {
                get {
                    var rdr = this.bextBlock.Target as StreamReader;
                    return (rdr == null ? ""raw-bytes"" : rdr.CurrentEncoding.BodyName);
                }
            }

            public long Offset = -1;

            public readonly Encoding Encoding;

            public BufferWithEncodingAndByteCounter(Stream stream, int codePage) {
                var fStrm = (stream as FileStream);
                if (fStrm != null)
                    this.FileName = fStrm.Name;
                this.bextBlock = BlockReaderFactory.Get(stream, codePage);
                this.Encoding = Encoding.GetEncoding(codePage);
            }

            /// <summary>
            ///     Marks a conservative lower bound for the buffer,
            ///     allowing space to be reclaimed.  If an application
            ///     needs to call GetString at arbitrary past locations
            ///     in the input stream, Mark() is not called.
            /// </summary>
            public override void Mark() {
                this.data.Mark(this.bPos - 2);
            }

            public override int Pos {
                get { return this.bPos; }
                set { this.bPos = value; }
            }

            private readonly char[] aux = new char[1];

            /// <summary>
            ///     Read returns the ordinal number of the next char, or
            ///     EOF (-1) for an end of stream.  Note that the next
            ///     code point may require *two* calls of Read().
            /// </summary>
            /// <returns></returns>
            public override int Read() {
                //
                //  Characters at positions 
                //  [data.offset, data.offset + data.bldr.Length)
                //  are available in data.bldr.
                //
                char c;

                if (this.bPos < this.data.MaxIndex) {
                    c = this.data[this.bPos++];
                    this.aux[0] = c;
                    this.Offset += this.Encoding.GetByteCount(this.aux);
                    return c;
                }

                // Experimental code, blocks of page size
                var chrs = new char[4096];
                var count = this.bextBlock(chrs, 0, 4096);

                if (count == 0)
                    return EndOfFile;

                this.data.Append(chrs, count);

                c = this.data[this.bPos++];

                this.aux[0] = c;
                this.Offset += this.Encoding.GetByteCount(this.aux);

                return c;
            }

            public override string GetString(int begin, int limit) {
                return this.data.GetString(begin, limit);
            }

            public override string ToString() {
                return ""StringBuilder buffer, encoding: "" + this.EncodingName;
            }
        }";

        private const string SegmentsScannerTypeName = "LogWatch.Features.Formats.Lex.Segments.Scanner";
        private const string RecordsScannerTypeName = "LogWatch.Features.Formats.Lex.Records.Scanner";

        private const string SegmentsScannerClassCode = @"
            private long segmentOffset;

            public override void SetSourceWithEncoding(Stream source, int codePage) {
                this.buffer = new BufferWithEncodingAndByteCounter(source, codePage);
                this.lNum = 0;
                this.code = '\n';
                this.GetCode();
            }

            public long CurrentOffset {
                get {
                    var bufferWithByCounter = this.buffer as BufferWithEncodingAndByteCounter;
                    if (bufferWithByCounter != null)
                        return bufferWithByCounter.Offset;
                    return -1;
                }
            }

            public int TextSize {
                get {
                    var bufferWithByCounter = this.buffer as BufferWithEncodingAndByteCounter;
                    if (bufferWithByCounter != null)
                        return bufferWithByCounter.Encoding.GetByteCount(this.yytext);
                    return -1;
                }
            }
            
            public void BeginSegment() { this.segmentOffset = this.CurrentOffset - this.TextSize; }

            public void EndSegment() {
                var length = (int) (this.CurrentOffset - this.TextSize - this.segmentOffset);
                this.NextOffset(this.segmentOffset, length);
            }";

        private const string CommonScannerClassCode = @"
            protected override string Text { get { return this.yytext; } }

            public override void Begin() {
                this.BEGIN(INITIAL);
            }

            public override int Parse(System.Threading.CancellationToken ct) {
                int next;
                do { next = Scan(); } while (!ct.IsCancellationRequested && next >= parserMax);
                return next;
            }";

        private const string RecordsScannerClassCode = @"
            public override void SetSourceWithEncoding(Stream source, int codePage) {
                this.SetSource(source, codePage);
            }";

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
            Stream saveAssemblyTo = null) {
            segmentScannerCode = Trim(segmentScannerCode);
            recordScannerCode = Trim(recordScannerCode);

            var result = new LexFormatScanners();

            this.Diagnostics.WriteLine("Compiling segments scanner");

            var name = string.Format("LexFileFormat-{0}", Guid.NewGuid().ToString("N"));

            var segmentsSyntaxTree = this.CreateSyntaxTree(
                new StringBuilder()
                    .AppendFormat("%using {0};\n", typeof (ScanBase).Namespace).AppendLine()
                    .AppendLine("%namespace LogWatch.Features.Formats.Lex.Segments")
                    .AppendLine("%{")
                    .AppendLine(BufferWithEncodingAndByteCounterCode)
                    .AppendLine(SegmentsScannerClassCode)
                    .AppendLine(CommonScannerClassCode)
                    .AppendLine("%}")
                    .AppendLine()
                    .AppendLine(segmentScannerCode)
                    .ToString());

            if (segmentsSyntaxTree == null)
                return result;

            this.Diagnostics.WriteLine("Compiling records scanner");

            var recordsSyntaxTree = this.CreateSyntaxTree(
                new StringBuilder()
                    .AppendFormat("%using {0};\n", typeof (ScanBase).Namespace)
                    .AppendLine("%namespace LogWatch.Features.Formats.Lex.Records")
                    .AppendLine("%{")
                    .AppendLine(RecordsScannerClassCode)
                    .AppendLine(CommonScannerClassCode)
                    .AppendLine("%}")
                    .AppendLine(recordScannerCode)
                    .ToString());

            if (recordsSyntaxTree == null)
                return result;

            this.Diagnostics.WriteLine("Creating assembly");

            var compilation = this.CreateCompilation(name, segmentsSyntaxTree, recordsSyntaxTree);

            var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName(name), AssemblyBuilderAccess.RunAndCollect);

            var module = assembly.DefineDynamicModule(name);

            var emitResult = compilation.Emit(module);

            foreach (var diagnostic in emitResult.Diagnostics)
                this.Diagnostics.WriteLine(diagnostic);

            if (!emitResult.Success)
                return result;

            if (saveAssemblyTo != null)
                compilation.Emit(saveAssemblyTo);

            return new LexFormatScanners {
                SegmentsScannerType = assembly.GetType(SegmentsScannerTypeName),
                RecordsScannerType = assembly.GetType(RecordsScannerTypeName),
                Success = true
            };
        }

        public LexFormatScanners LoadCompiled(Stream stream) {
            try {
                var memoryStream = new MemoryStream(4096);

                stream.CopyTo(memoryStream);

                var assembly = Assembly.Load(memoryStream.ToArray());

                return new LexFormatScanners {
                    SegmentsScannerType = assembly.GetType(SegmentsScannerTypeName),
                    RecordsScannerType = assembly.GetType(RecordsScannerTypeName),
                    Success = true
                };
            } catch (FileLoadException) {
                return new LexFormatScanners {Success = false};
            } catch (BadImageFormatException) {
                return new LexFormatScanners {Success = false};
            }
        }

        private string CompileLex(string code) {
            var lexFile = Path.ChangeExtension(Path.GetTempFileName(), ".lex");
            var codeFile = Path.ChangeExtension(lexFile, ".cs");

            File.WriteAllText(lexFile, code, Encoding.UTF8);

            var process = Process.Start(new ProcessStartInfo {
                FileName = "gplex.exe",
                Arguments =
                    string.Format("/verbose /version /noPersistBuffer /unicode /codePage:65001 /out:{0} {1}", codeFile,
                        lexFile),
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
            public bool Success { get; set; }
        }
    }
}