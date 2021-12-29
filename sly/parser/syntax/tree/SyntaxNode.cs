using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using sly.parser.generator;
using sly.parser.syntax.grammar;

namespace sly.parser.syntax.tree
{
    public class SyntaxNode<IN,OUT> : ISyntaxNode<IN,OUT> where IN : struct
    {
        public SyntaxNode(string name, List<ISyntaxNode<IN, OUT>> children = null, MethodInfo visitor = null, CallVisitor<OUT> visitorCaller = null )
        {
            Name = name;
            Children = children ?? new List<ISyntaxNode<IN, OUT>>();
            Visitor = visitor;
            VisitorCaller = visitorCaller;
        }

        public List<ISyntaxNode<IN, OUT>> Children { get; }

        public MethodInfo Visitor { get; set; }
        public CallVisitor<OUT> VisitorCaller { get; set; }
        

        public bool IsByPassNode { get; set; } = false;

        public bool IsEmpty => Children == null || !Children.Any();

        public Affix ExpressionAffix { get; set; }


        public bool Discarded => false;
        public string Name { get; set; }

        public bool HasByPassNodes { get; set; } = false;

        #region expression syntax nodes

        public OperationMetaData<IN, OUT> Operation { get; set; } = null;

        public bool IsExpressionNode => Operation != null;

        public bool IsBinaryOperationNode => IsExpressionNode && Operation.Affix == Affix.InFix;
        public bool IsUnaryOperationNode => IsExpressionNode && Operation.Affix != Affix.InFix;
        public int Precedence => IsExpressionNode ? Operation.Precedence : -1;

        public Associativity Associativity =>
            IsExpressionNode && IsBinaryOperationNode ? Operation.Associativity : Associativity.None;

        public bool IsLeftAssociative => Associativity == Associativity.Left;

        public ISyntaxNode<IN, OUT> Left
        {
            get
            {
                ISyntaxNode<IN, OUT> l = null;
                if (IsExpressionNode)
                {
                    var leftindex = -1;
                    if (IsBinaryOperationNode) leftindex = 0;
                    if (leftindex >= 0) l = Children[leftindex];
                }

                return l;
            }
        }

        public ISyntaxNode<IN, OUT> Right
        {
            get
            {
                ISyntaxNode<IN, OUT> r = null;
                if (IsExpressionNode)
                {
                    var rightIndex = -1;
                    if (IsBinaryOperationNode)
                        rightIndex = 2;
                    else if (IsUnaryOperationNode) rightIndex = 1;
                    if (rightIndex > 0) r = Children[rightIndex];
                }

                return r;
            }
        }
        
        public string Dump(string tab)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"{tab}+ {Name} {(IsByPassNode ? "===":"")}");
            foreach (var child in Children)
            {
                builder.AppendLine($"{child.Dump(tab + "\t")}");
            }

            return builder.ToString();
        }


        #endregion
    }
}