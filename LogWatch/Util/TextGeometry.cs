using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace LogWatch.Util {
    public sealed class TextGeometry : MarkupExtension {
        public TextGeometry(string text) {
            this.Text = text;
            this.FontFamily = new FontFamily("Segoe UI");
            this.FontSize = 12;
            this.Brush = Brushes.Black;
        }

        public TextGeometry() {
        }

        [ConstructorArgument("Text")]
        public string Text { get; set; }

        public FontFamily FontFamily { get; set; }
        public FontStyle FontStyle { get; set; }
        public FontWeight FontWeight { get; set; }
        public FontStretch FontStretch { get; set; }

        [TypeConverter(typeof (FontSizeConverter))]
        public double FontSize { get; set; }

        public Brush Brush { get; set; }

        public FlowDirection FlowDirection { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider) {
            var text = new FormattedText(
                this.Text,
                CultureInfo.CurrentCulture,
                this.FlowDirection,
                new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch),
                this.FontSize,
                this.Brush);

            return text.BuildGeometry(new Point(0, 0));
        }
    }
}