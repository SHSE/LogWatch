using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace LogWatch.Features.Formats {
    public partial class LexView {
        public LexView() {
            RegisterDefinition("Lex.xshd");

            this.InitializeComponent();
        }

        public LexViewModel ViewModel {
            get { return (LexViewModel) this.DataContext; }
        }

        private static void RegisterDefinition(string fileName) {
            var definition = HighlightingLoader.Load(new XmlTextReader(fileName), HighlightingManager.Instance);
            HighlightingManager.Instance.RegisterHighlighting(definition.Name, new string[0], definition);
        }
    }
}