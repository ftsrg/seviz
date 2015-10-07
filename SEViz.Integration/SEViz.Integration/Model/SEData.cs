using SEViz.Common;
using SEViz.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEViz.Integration.Model
{
    public class SEData
    {
        // Contains the runs, key - last node of the run, value - list of nodes
        public Dictionary<SENode,List<SENode>> Runs { get; private set; }

        public SEData()
        {
            Runs = new Dictionary<SENode, List<SENode>>();
        }
    }
}
