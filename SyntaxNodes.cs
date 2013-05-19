using System.Collections.Generic;
using System.Linq;

namespace Compiler
{
    public interface ISyntaxNode
    {
        IEnumerable<ISyntaxNode> GetChildren();
    }

    public class ProgramNode : ISyntaxNode
    {
        public List<FunctionNode> Functions { get; private set; }

        public ProgramNode()
        {
            Functions = new List<FunctionNode>();
        }

        public IEnumerable<ISyntaxNode> GetChildren()
        {
            return Functions;
        }
    }

    public class FunctionNode : ISyntaxNode
    {
        public IdentifierNode Name { get; private set; }
        public List<IdentifierNode> Parameters { get; private set; }
        public BlockNode Body { get; set; }

        public FunctionNode(IdentifierNode name)
        {
            Name = name;
            Parameters = new List<IdentifierNode>();
        }

        public IEnumerable<ISyntaxNode> GetChildren()
        {
            return new ISyntaxNode[] {Name}.Concat(Parameters).Concat(new[] {Body});
        }
    }

    public abstract class StatementNode : ISyntaxNode
    {
        public abstract IEnumerable<ISyntaxNode> GetChildren();
    }

    public class BlockNode : StatementNode
    {
        public List<DeclarationNode> Declarations { get; private set; }
        public List<StatementNode> Statements { get; private set; }

        public BlockNode()
        {
            Declarations = new List<DeclarationNode>();
            Statements = new List<StatementNode>();
        }

        public override IEnumerable<ISyntaxNode> GetChildren()
        {
            return Declarations.Cast<ISyntaxNode>().Concat(Statements);
        }
    }

    public class AssignmentStatementNode : StatementNode
    {
        public IdentifierNode Variable { get; private set; }
        public ExpressionNode Expression { get; private set; }

        public AssignmentStatementNode(IdentifierNode variable, ExpressionNode expression)
        {
            Variable = variable;
            Expression = expression;
        }


        public override IEnumerable<ISyntaxNode> GetChildren()
        {
            return new ISyntaxNode[] {Variable, Expression};
        }
    }

    public class ReturnStatementNode : StatementNode
    {
        public ExpressionNode Expression { get; private set; }

        public ReturnStatementNode(ExpressionNode expression)
        {
            Expression = expression;
        }

        public override IEnumerable<ISyntaxNode> GetChildren()
        {
            return new[] {Expression};
        }
    }

    public enum NullStatementType
    {
        Continue,
        Break,
    }

    public class NullStatementNode : StatementNode
    {
        public NullStatementType Type { get; private set; }

        public NullStatementNode(NullStatementType type)
        {
            Type = type;
        }

        public override IEnumerable<ISyntaxNode> GetChildren()
        {
            return new ISyntaxNode[0];
        }
    }

    public class IfStatementNode : StatementNode
    {
        public ExpressionNode Condition { get; private set; }
        public StatementNode ThenBody { get; private set; }
        public StatementNode ElseBody { get; private set; }

        public IfStatementNode(ExpressionNode condition, StatementNode thenBody)
        {
            Condition = condition;
            ThenBody = thenBody;
            ElseBody = null;
        }

        public IfStatementNode(ExpressionNode condition, StatementNode thenBody, StatementNode elseBody)
        {
            Condition = condition;
            ThenBody = thenBody;
            ElseBody = elseBody;
        }

        public override IEnumerable<ISyntaxNode> GetChildren()
        {
            var children = new List<ISyntaxNode> {Condition, ThenBody};
            if (ElseBody != null)
                children.Add(ElseBody);
            return children;
        }
    }

    public class WhileStatementNode : StatementNode
    {
        public ExpressionNode Condition { get; private set; }
        public StatementNode Body { get; private set; }

        public WhileStatementNode(ExpressionNode condition, StatementNode body)
        {
            Condition = condition;
            Body = body;
        }

        public override IEnumerable<ISyntaxNode> GetChildren()
        {
            return new ISyntaxNode[] {Condition, Body};
        }
    }

    public class PrintStatementNode : StatementNode
    {
        public List<IPrintItemNode> Items { get; private set; }

        public PrintStatementNode()
        {
            Items = new List<IPrintItemNode>();
        }

        public override IEnumerable<ISyntaxNode> GetChildren()
        {
            return Items;
        }
    }

    public interface IPrintItemNode : ISyntaxNode
    {
    }

    public abstract class ExpressionNode : ISyntaxNode, IPrintItemNode
    {
        public abstract IEnumerable<ISyntaxNode> GetChildren();
    }

    public enum Operator
    {
        Equal,
        NotEqual,
        LessThan,
        LessThanOrEqualTo,
        GreaterThan,
        GreaterThanOrEqualTo,
        Add,
        Subtract,
        Multiply,
        Divide,
    }

    public class BinaryExpressionNode : ExpressionNode
    {
        public ExpressionNode Left { get; private set; }
        public Operator Operator { get; private set; }
        public ExpressionNode Right { get; private set; }

        public BinaryExpressionNode(ExpressionNode left, Operator @operator, ExpressionNode right)
        {
            Left = left;
            Operator = @operator;
            Right = right;
        }

        public override IEnumerable<ISyntaxNode> GetChildren()
        {
            return new[] {Left, Right};
        }
    }

    public class UnaryExpressionNode : ExpressionNode
    {
        public Operator Operator { get; private set; }
        public ExpressionNode Child { get; private set; }

        public UnaryExpressionNode(Operator @operator, ExpressionNode child)
        {
            Operator = @operator;
            Child = child;
        }

        public override IEnumerable<ISyntaxNode> GetChildren()
        {
            return new[] {Child};
        }
    }

    public class FunctionCallNode : ExpressionNode
    {
        public IdentifierNode Name { get; private set; }
        public List<ExpressionNode> Arguments { get; private set; }

        public FunctionCallNode(IdentifierNode name)
        {
            Name = name;
            Arguments = new List<ExpressionNode>();
        }

        public override IEnumerable<ISyntaxNode> GetChildren()
        {
            return new ISyntaxNode[] {Name}.Concat(Arguments);
        }
    }

    public class ConstantExpressionNode : ExpressionNode
    {
        public IntegerNode Value { get; private set; }

        public ConstantExpressionNode(IntegerNode value)
        {
            Value = value;
        }

        public override IEnumerable<ISyntaxNode> GetChildren()
        {
            return new[] {Value};
        }
    }

    public class VariableReferenceNode : ExpressionNode
    {
        public IdentifierNode Variable { get; private set; }

        public VariableReferenceNode(IdentifierNode variable)
        {
            Variable = variable;
        }

        public override IEnumerable<ISyntaxNode> GetChildren()
        {
            return new[] {Variable};
        }
    }

    public class DeclarationNode : ISyntaxNode
    {
        public List<IdentifierNode> Variables { get; private set; }

        public DeclarationNode()
        {
            Variables = new List<IdentifierNode>();
        }

        public IEnumerable<ISyntaxNode> GetChildren()
        {
            return Variables;
        }
    }

    public class IdentifierNode : ISyntaxNode
    {
        public string Name { get; private set; }

        public IdentifierNode(string name)
        {
            Name = name;
        }

        public IEnumerable<ISyntaxNode> GetChildren()
        {
            return new ISyntaxNode[0];
        }
    }

    public class IntegerNode : ISyntaxNode
    {
        public int Value { get; private set; }

        public IntegerNode(string value)
        {
            Value = int.Parse(value);
        }

        public IEnumerable<ISyntaxNode> GetChildren()
        {
            return new ISyntaxNode[0];
        }
    }

    public class StringNode : ISyntaxNode, IPrintItemNode
    {
        public string Value { get; private set; }

        public StringNode(string value)
        {
            Value = value;
        }

        public IEnumerable<ISyntaxNode> GetChildren()
        {
            return new ISyntaxNode[0];
        }
    }
}
