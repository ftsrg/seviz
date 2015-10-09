using SEViz.Common.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SEViz.Integration.Helpers.Converters
{
    public class EdgeColorToForegroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var color = (SEEdge.EdgeColor)value;
            switch(color)
            {
                case SEEdge.EdgeColor.Black:
                    return "Silver";
                case SEEdge.EdgeColor.Red:
                    return "Red";
                default:
                    return "Silver";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
