using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace LogWatch.Features.RecordDetails {
    [StyleTypedProperty(Property = "ContainerStyle", StyleTargetType = typeof (RichTextBox))]
    [StyleTypedProperty(Property = "HyperlinkStyle", StyleTargetType = typeof (Hyperlink))]
    public class ExceptionHighlighter : Freezable, IValueConverter {
        private static readonly Regex Header = new Regex(@"^(?<NS>(?:\w+[.])+)(?<Class>\w+): (?<Message>.*)");

        private static readonly Regex StackTraceItem = new Regex(
            @"^\s+\w+\s+(?<NS>(?:\w+[.])+)(?<Class>[\w\[\],<>]+)[.](?<Method><.ctor>\w+|[\w\[\],<>]+)[(](?<Params>[^)]+)?[)](?:\s+\w+\s+(?<Url>.+):\w+\s+(?<Line>\d+))?");

        public static readonly DependencyProperty NavigateToUrlCommandProperty =
            DependencyProperty.Register("NavigateToUrlCommand", typeof (ICommand), typeof (ExceptionHighlighter));

        public static readonly DependencyProperty HyperlinkStyleProperty =
            DependencyProperty.Register("HyperlinkStyle", typeof (Style), typeof (ExceptionHighlighter));

        public static readonly DependencyProperty ContainerStyleProperty =
            DependencyProperty.Register("ContainerStyle", typeof (Style), typeof (ExceptionHighlighter));

        public Brush Namespace { get; set; }
        public Brush Method { get; set; }
        public Brush Message { get; set; }
        public Brush MethodParameterType { get; set; }
        public Brush Class { get; set; }
        public Brush String { get; set; }
        public Brush Line { get; set; }

        public ICommand NavigateToUrlCommand {
            get { return (ICommand) this.GetValue(NavigateToUrlCommandProperty); }
            set { this.SetValue(NavigateToUrlCommandProperty, value); }
        }

        public Style ContainerStyle {
            get { return (Style) this.GetValue(ContainerStyleProperty); }
            set { this.SetValue(ContainerStyleProperty, value); }
        }

        public Style HyperlinkStyle {
            get { return (Style) this.GetValue(HyperlinkStyleProperty); }
            set { this.SetValue(HyperlinkStyleProperty, value); }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return this.CreateTextBlock((string) value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }

        private RichTextBox CreateTextBlock(string exception) {
            if (string.IsNullOrEmpty(exception))
                return null;

            var result = new Paragraph();
            var lines = exception.Split(new[] {Environment.NewLine}, StringSplitOptions.None);

            foreach (var line in lines) {
                var span = new Span();

                if (!this.TryHeader(line, span) && !this.TryStackTraceItem(line, span))
                    span.Inlines.Add(line);

                span.Inlines.Add(new LineBreak());
                result.Inlines.Add(span);
            }

            return new RichTextBox(new FlowDocument(result)) {
                IsReadOnly = true,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                Background = null,
                BorderThickness = new Thickness(0),
                Style = this.ContainerStyle
            };
        }

        private bool TryStackTraceItem(string line, Span span) {
            var match = StackTraceItem.Match(line);
            if (match.Success) {
                span.Inlines.Add("   at ");
                span.Inlines.Add(new Run(match.Groups["NS"].Value) {Foreground = this.Namespace});
                span.Inlines.Add(new Run(match.Groups["Class"].Value) {Foreground = this.Class});
                span.Inlines.Add(".");
                span.Inlines.Add(new Run(match.Groups["Method"].Value) {Foreground = this.Method});
                span.Inlines.Add("(");
                span.Inlines.Add(new Run(match.Groups["Params"].Value));
                span.Inlines.Add(")");

                var url = match.Groups["Url"].Value;

                if (!string.IsNullOrEmpty(url)) {
                    var hyperlink = new Hyperlink(new Run(Path.GetFileName(url))) {
                        ToolTip = url,
                        Style = this.HyperlinkStyle
                    };

                    BindingOperations.SetBinding(hyperlink, Hyperlink.CommandProperty,
                        new Binding("NavigateToUrlCommand") {
                            Source = this,
                            Mode = BindingMode.OneWay
                        });

                    hyperlink.CommandParameter = url;

                    span.Inlines.Add(" in ");
                    span.Inlines.Add(hyperlink);
                    span.Inlines.Add(":line ");
                    span.Inlines.Add(new Run(match.Groups["Line"].Value) {Foreground = this.Line});
                }
                return true;
            }
            return false;
        }

        private bool TryHeader(string line, Span span) {
            var match = Header.Match(line);

            if (match.Success) {
                span.Inlines.Add(new Run(match.Groups["NS"].Value) {Foreground = this.Namespace});
                span.Inlines.Add(new Run(match.Groups["Class"].Value) {Foreground = this.Class});
                span.Inlines.Add(": ");
                span.Inlines.Add(new Run(match.Groups["Message"].Value) {Foreground = this.Message});
                return true;
            }

            return false;
        }

        protected override Freezable CreateInstanceCore() {
            return this;
        }
    }
}