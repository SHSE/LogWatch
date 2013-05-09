using System.Windows.Data;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace LogWatch.Features.Formats {
    public partial class LexEditView {
        public LexEditView() {
            RegisterDefinition("Lex.xshd");

            this.Buttons = new[] {this.OkButton, this.CancelButton};

            this.InitializeComponent();

            BindingOperations.SetBinding(this.OkButton, IsEnabledProperty,
                new Binding("IsCompiled") {Source = this.DataContext});
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