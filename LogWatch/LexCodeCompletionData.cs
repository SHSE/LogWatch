using System;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace LogWatch {
    public class LexCodeCompletionData : ICompletionData {
        private readonly string content;

        public LexCodeCompletionData(string text, string content = null) {
            this.Text = text;
            this.content = content ?? text;
        }

        public void Complete(TextArea textArea, ISegment completionSegment, EventArgs e) {
            textArea.Document.Replace(completionSegment, this.content);
        }

        public ImageSource Image {
            get { return null; }
        }

        public string Text { get; private set; }

        public object Content {
            get { return this.content; }
        }

        public object Description { get; set; }
        public double Priority { get; set; }
    }
}