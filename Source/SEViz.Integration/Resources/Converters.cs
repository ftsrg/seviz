using SEViz.Common;
using SEViz.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SEViz.Integration.Resources
{
    public static class Converters
    {
        public static Color SevizColorToWpfColor(SENode.NodeColor nodeColor)
        {
            switch(nodeColor)
            {
                case SENode.NodeColor.Green:
                    return Colors.Green;
                case SENode.NodeColor.Orange:
                    return Colors.Orange;
                case SENode.NodeColor.Red:
                    return Colors.Red;
                case SENode.NodeColor.White:
                    return Colors.White;
                case SENode.NodeColor.Indigo:
                    return Colors.Indigo;
                case SENode.NodeColor.Blue:
                    return Colors.RoyalBlue;
                default:
                    return Colors.White;
            }
        }
    }
}
