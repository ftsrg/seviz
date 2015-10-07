using GraphSharp;
using QuickGraph;
using QuickGraph.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SEViz.Common.Model
{
    public class SEGraph : SoftMutableBidirectionalGraph<SENode, SEEdge>
    {
        public static void Serialize(SEGraph graph, string path)
        {
            var ser = new GraphMLSerializer<SENode, SEEdge, SEGraph>();
            using (var writer = XmlWriter.Create(path, new XmlWriterSettings { Indent = true, WriteEndDocumentOnClose = false }))
            {
                ser.Serialize(writer, graph, v => v.Id.ToString(), e => e.Id.ToString());
            }
        }

        public static SEGraph Deserialize(string path)
        {
            var deser = new GraphMLDeserializer<SENode, SEEdge, SEGraph>();
            var graph = new SEGraph();
            var ivf = new IdentifiableVertexFactory<SENode>(SENode.Factory);
            var ief = new IdentifiableEdgeFactory<SENode, SEEdge>(SEEdge.Factory);
            using (var reader = XmlReader.Create(path))
            {
                deser.Deserialize(reader, graph, ivf, ief);
            }
            return graph;
        }
    }
   
}
