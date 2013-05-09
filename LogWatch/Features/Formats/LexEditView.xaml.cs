using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace LogWatch.Features.Formats {
    public partial class LexEditView {
        public LexEditView() {
            RegisterDefinition("Lex.xshd");

            this.Buttons = new[] {this.OkButton, this.CancelButton};

            this.InitializeComponent();
        }

        public LexEditViewModel ViewModel {
            get { return (LexEditViewModel) this.DataContext; }
        }

        private static void RegisterDefinition(string fileName) {
            var definition = HighlightingLoader.Load(new XmlTextReader(fileName), HighlightingManager.Instance);
            HighlightingManager.Instance.RegisterHighlighting(definition.Name, new string[0], definition);
        }
    }
}