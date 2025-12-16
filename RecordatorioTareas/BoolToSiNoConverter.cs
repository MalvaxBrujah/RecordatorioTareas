using System;
using System.Globalization;
using System.Windows.Data;

namespace RecordatorioTareas
{
    public class BoolToSiNoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? "Sí" : "No";

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // No lo vamos a usar para edición, así que simplemente devolvemos false
            return Binding.DoNothing;
        }
    }
}
