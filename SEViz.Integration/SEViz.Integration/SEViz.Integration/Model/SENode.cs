using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEViz.Integration.Model
{
    public class SENode
    {
        public enum NodeColor
        {
            White,
            Green,
            Orange,
            Red
        }

        public enum NodeShape
        {
            Ellipse,
            Rectangle
        }

        public enum NodeBorder
        {
            Single,
            Double
        }

        #region Appearance

        public NodeColor Color { get; private set; }

        public NodeShape Shape { get; private set; }

        public NodeBorder Border { get; private set; }

        #endregion

        #region Metadata

        public int Id { get; private set; }

        public string PathCondition { get; private set; }

        public string IncrementalPathCondition { get; private set; }

        public string MethodName { get; private set; }

        public Tuple<string, int> SourceCodeMapping { get; set; }

        public string GenerateTestCode { get; private set; }

        public string Status { get; private set; }

        public HashSet<SENode> CollapsedSubtreeNodes { get; private set; }

        public HashSet<SEEdge> CollapsedSubtreeEdges { get; private set; }

        #endregion

        public SENode(int id, string pathCondition, string methodName, Tuple<string, int> sourceCodeMapping, string generatedTestCode, string status)
        {
            // TODO implement constructor for SENode
            Id = id;
            CollapsedSubtreeNodes = new HashSet<SENode>();
            CollapsedSubtreeEdges = new HashSet<SEEdge>();
        }

    }
}
