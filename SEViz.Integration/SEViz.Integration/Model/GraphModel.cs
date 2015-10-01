using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEViz.Integration.Model
{
    public class GraphModel
    {

        public GraphModel(SEGraph graph/*, SEGraphLayout layout*/)
        {
            Graph = graph;
            /*Layout = layout;*/
        }

        //public SEGraphLayout Layout { get; private set; }
        public SEGraph Graph { get; private set; }

        public SEData Data { get; private set; }
    }
}
