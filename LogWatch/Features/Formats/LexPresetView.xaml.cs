using System.Collections.Generic;
using System.Windows.Input;
using System.Xml;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace LogWatch.Features.Formats {
    public partial class LexPresetView {
        private CompletionWindow completionWindow;

        public LexPresetView() {
            RegisterDefinition("Lex.xshd");

            this.InitializeComponent();

            this.SegmentCodeEditor.TextArea.TextEntered +=
                (sender, args) =>
                this.ShowCompletionWindow(
                    args,
                    this.ViewModel.SegmentCodeCompletion, 
                    this.SegmentCodeEditor);

            this.RecordCodeEditor.TextArea.TextEntered +=
                (sender, args) =>
                this.ShowCompletionWindow(
                    args,
                    this.ViewModel.RecordCodeCompletion, 
                    this.RecordCodeEditor);

            this.SegmentCodeEditor.TextArea.TextEntering += this.OnTextAreaOnTextEntering;
            this.RecordCodeEditor.TextArea.TextEntering += this.OnTextAreaOnTextEntering;
        }

        public LexPresetViewModel ViewModel {
            get { return (LexPresetViewModel) this.DataContext; }
        }

        private void ShowCompletionWindow(
            TextCompositionEventArgs args,
            IEnumerable<LexCodeCompletionData> lexCodeCompletionDatas, 
            TextEditor editor) {
            if (args.Text == " " && Keyboard.IsKeyDown(Key.LeftCtrl) || args.Text == "." || args.Text == "<" ||
                args.Text == "{") {
                if (args.Text == " " && Keyboard.IsKeyDown(Key.LeftCtrl))
                    editor.TextArea.Document.Remove(editor.CaretOffset - 1, 1);

                this.completionWindow = new CompletionWindow(editor.TextArea);

                foreach (var completionData in lexCodeCompletionDatas)
                    this.completionWindow.CompletionList.CompletionData.Add(completionData);

                this.completionWindow.Closed += delegate { this.completionWindow = null; };
                this.completionWindow.Show();
            }
        }

        private void OnTextAreaOnTextEntering(object sender, TextCompositionEventArgs args) {
            if (args.Text.Length > 0 && this.completionWindow != null && !char.IsLetterOrDigit(args.Text[0]))
                this.completionWindow.CompletionList.RequestInsertion(args);
        }

        private static void RegisterDefinition(string fileName) {
            var definition = HighlightingLoader.Load(new XmlTextReader(fileName), HighlightingManager.Instance);
            HighlightingManager.Instance.RegisterHighlighting(definition.Name, new string[0], definition);
        }
    }
}