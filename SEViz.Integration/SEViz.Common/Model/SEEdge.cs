using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEViz.Common.Model
{
    public class SEEdge : Edge<SENode>
    {
        public int Id { get; private set; }

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
