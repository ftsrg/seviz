using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SEViz.Integration.Model
{
    public class SENode
    {

        public enum NodeColor
        {
            White,
            Green,
            Orange,
            Red,
            Indigo,
            Blue,
            None
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


        private Stack<NodeColor> originalColors = new Stack<NodeColor>();

        
        internal NodeColor Color { get; private set; }

        public NodeShape Shape { get; private set; }

        internal NodeBorder Border { get; private set; }
        
        internal bool IsSelected { get; private set; }

        #endregion

        #region Metadata

        [Category("Node details")]
        [DisplayName("Id")]
        [Description("Identifier of the node indicating its place in execution order")]
        public int Id { get; private set; }

        [Category("Node details")]
        [DisplayName("Path condition")]
        [Description("Full form of the path condition that contains all constraints from the start")]
        public string PathCondition { get; private set; }

        [Category("Node details")]
        [DisplayName("Incremental path condition")]
        [Description("Incremental form of the path condition, compared to its parent node")]
        public string IncrementalPathCondition { get; private set; }

        [Category("Node details")]
        [DisplayName("Method name")]
        [Description("Fully qualified name of the method that contains this node")]
        public string MethodName { get; private set; }

        [Category("Node details")]
        [DisplayName("Source code mapping")]
        [Description("Aprroximation of the place in the source code")]
        public Tuple<string, int> SourceCodeMapping { get; set; }

        [Category("Node details")]
        [DisplayName("Generated test code")]
        [Description("The generated source code if it is a leaf node and a test was generated (e.g., not a duplicate)")]
        public string GenerateTestCode { get; private set; }

        [Category("Node details")]
        [DisplayName("Node status")]
        [Description("Status of the node indicating whether there are remaining uncovered branches starting from this node")]
        public string Status { get; private set; }

        internal HashSet<SENode> CollapsedSubtreeNodes { get; private set; }

        internal HashSet<SEEdge> CollapsedSubtreeEdges { get; private set; }

        #endregion

        public SENode(int id, string pathCondition, string methodName, Tuple<string, int> sourceCodeMapping, string generatedTestCode, string status, bool solverCall)
        {
            // TODO implement constructor for SENode
            Id = id;
            CollapsedSubtreeNodes = new HashSet<SENode>();
            CollapsedSubtreeEdges = new HashSet<SEEdge>();
            Color = NodeColor.White;
            originalColors.Push(NodeColor.White);
            IsSelected = false;
            Shape = (solverCall) ?  NodeShape.Ellipse : NodeShape.Rectangle;
        }

        #region Public methods

        public void Collapse()
        {
            NodeColor o;
            if (originalColors.Count == 1) o = originalColors.Peek();
            else o = originalColors.Pop();
            if (o != NodeColor.Indigo)
            {
                originalColors.Push(Color);
                Color = NodeColor.Indigo;
            }
        }

        public void Expand()
        {
            if(IsSelected)
            {
                originalColors.Pop();
            }
            RevertToOriginalColor();
        }

        public void RevertToOriginalColor()
        {
            NodeColor o;
            if (originalColors.Count == 1) o = originalColors.Peek();
            else o = originalColors.Pop();
            if (o != Color)
            {
                Color = o;
            }
        }

        public void Select()
        {
            IsSelected = true;
            NodeColor o;
            if (originalColors.Count == 1) o = originalColors.Peek();
            else o = originalColors.Pop();
            if (o != NodeColor.Blue)
            {
                originalColors.Push(Color);
                Color = NodeColor.Blue;
            }
        }

        public void Deselect()
        {
            IsSelected = false;
            RevertToOriginalColor();
        }

        public override string ToString()
        {
            return Id.ToString();
        }

        #endregion

    }
}
