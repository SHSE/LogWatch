using System;
using System.Globalization;
using System.Windows.Data;
using System.Linq;

namespace LogWatch.Features.Records {
    public class LoggerToShortStringConverter  : IValueConverter{
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var logger = value as string;

            if (string.IsNullOrEmpty(logger))
                return null;

            return logger.Split('.').LastOrDefault();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }
}