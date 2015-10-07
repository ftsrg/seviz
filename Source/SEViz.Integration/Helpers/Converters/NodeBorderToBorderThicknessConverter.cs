using SEViz.Common;
using SEViz.Common.Model;
using SEViz.Integration.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SEViz.Integration.Helpers.Converters
{
    public class NodeBorderToBorderThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var border = (SENode.NodeBorder)value;
            switch (border)
            {
                case SENode.NodeBorder.Single:
                    return 0;
                case SENode.NodeBorder.Double:
                    return 1;
                default:
                    return 0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
