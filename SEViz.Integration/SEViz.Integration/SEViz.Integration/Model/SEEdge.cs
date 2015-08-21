using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEViz.Integration.Model
{
    public class SEEdge : Edge<SENode>
    {
        public int Id { get; private set; }

        public SEEdge(int id, SENode source, SENode target) : base(source, target)
        {
            Id = id;
        }

    }
}
