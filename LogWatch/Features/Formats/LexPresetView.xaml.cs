using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace LogWatch.Features.Formats {
    public partial class LexPresetView {
        public LexPresetView() {
            RegisterDefinition("Lex.xshd");

            this.InitializeComponent();
        }

        public LexPresetViewModel ViewModel {
            get { return (LexPresetViewModel) this.DataContext; }
        }

        private static void RegisterDefinition(string fileName) {
            var definition = HighlightingLoader.Load(new XmlTextReader(fileName), HighlightingManager.Instance);
            HighlightingManager.Instance.RegisterHighlighting(definition.Name, new string[0], definition);
        }
    }
}