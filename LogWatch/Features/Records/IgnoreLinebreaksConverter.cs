using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace LogWatch.Features.Records {
    public class IgnoreLinebreaksConverter : IValueConverter {
        private const string Sub = " \u200B";
        private const string Lb = "\r\n";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var s = (string) value;
            return string.IsNullOrEmpty(s) ? s : Regex.Replace(s, Lb, Sub);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            var s = (string) value;
            return string.IsNullOrEmpty(s) ? s : Regex.Replace(s, Sub, Lb);
        }
    }
}