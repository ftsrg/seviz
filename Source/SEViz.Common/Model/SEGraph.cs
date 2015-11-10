/*
 * SEViz - Symbolic Execution VIsualiZation
 *
 * SEViz is a tool, which can support the test generation process by
 * visualizing the symbolic execution in a directed graph.
 *
 * Authors: Dávid Honfi <honfi@mit.bme.hu>, Zoltán Micskei
 * <micskeiz@mit.bme.hu>, András Vörös <vori@mit.bme.hu>
 * 
 * Copyright 2015 Budapest University of Technology and Economics (BME)
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 * 
 */

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
        private static void ReplaceLineBreaks(SEGraph graph, bool serialize)
        {
            if(serialize)
            {
                foreach(var v in graph.Vertices)
                {
                    v.PathCondition = v.PathCondition.Replace(Environment.NewLine, "[LB]");
                    v.IncrementalPathCondition = v.IncrementalPathCondition.Replace(Environment.NewLine, "[LB]");
                }
            } else
            {
                foreach(var v in graph.Vertices)
                {
                    v.PathCondition = v.PathCondition.Replace("[LB]", Environment.NewLine);
                    v.IncrementalPathCondition = v.IncrementalPathCondition.Replace("[LB]", Environment.NewLine);
                }
            }
        }

        public static void Serialize(SEGraph graph, string path)
        {
            ReplaceLineBreaks(graph, true);
            var ser = new GraphMLSerializer<SENode, SEEdge, SEGraph>();
            using (var writer = XmlWriter.Create(path+"temp.graphml", new XmlWriterSettings { Indent = true, WriteEndDocumentOnClose = false }))
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
            ReplaceLineBreaks(graph, false);
            return graph;
        }
    }
   
}
