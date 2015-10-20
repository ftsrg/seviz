using QuickGraph;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SEViz.Common.Model
{
    public class SEEdge : Edge<SENode>
    {
        public enum EdgeColor
        {
            Black,
            Red,
            Green
        }

        public int Id { get; private set; }

        [Browsable(false)]
        public EdgeColor Color { get; set; }

        [XmlAttribute("color")]
        [Browsable(false)]
        public int sColor { get { return (int)Color; } set { Color = (EdgeColor)Enum.ToObject(typeof(EdgeColor), value); } }

        public SEEdge(int id, SENode source, SENode target) : base(source, target)
        {
            Id = id;
        }

        public static SEEdge Factory(SENode source, SENode target, string id)
        {
            return new SEEdge(Int32.Parse(id), source, target);
        }

    }
    
}
