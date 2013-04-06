using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace LogWatch.Util {
    public sealed class BooleanToOppositeConverter : MarkupExtension, IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return !(bool) value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return !(bool) value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider) {
            return this;
        }
    }
}