using System.Collections.Generic;
using System.Linq;

namespace Compiler
{
    public interface ISyntaxNode
    {
        IEnumerable<ISyntaxNode> GetChildren();
        void Accept(ISyntaxNodeVisitor visitor);
    }

    public interface ISyntaxNodeVisitor
    {
        void Visit(ProgramNode program);
        void Visit(FunctionNode function);
        void Visit(BlockStatementNode block);
        void Visit(AssignmentStatementNode assignment);
        void Visit(ReturnStatementNode statement);
        void Visit(NullStatementNode statement);
        void Visit(IfStatementNode statement);
        void Visit(WhileStatementNode statement);
        void Visit(PrintStatementNode print);
        void Visit(InputStatementNode input);
        void Visit(BinaryExpressionNode expression);
        void Visit(UnaryExpressionNode expression);
        void Visit(FunctionCallNode call);
        void Visit(ConstantExpressionNode constant);
        void Visit(VariableReferenceNode reference);
        void Visit(DeclarationNode declaration);
        void Visit(IdentifierNode identifier);
        void Visit(IntegerNode integer);
        void Visit(StringNode str);
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

        public void Accept(ISyntaxNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class FunctionNode : ISyntaxNode
    {
        public IdentifierNode Name { get; private set; }
        public List<IdentifierNode> Parameters { get; private set; }
        public BlockStatementNode Body { get; set; }
        public SymbolTableEntry SymbolTableEntry { get; set; }
        public int LocalVariableCount { get; set; }

        public FunctionNode(IdentifierNode name)
        {
            Name = name;
            Parameters = new List<IdentifierNode>();
        }

        public IEnumerable<ISyntaxNode> GetChildren()
        {
            return new ISyntaxNode[] {Name}.Concat(Parameters).Concat(new[] {Body});
        }
        
        public void Accept(ISyntaxNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return "Function: " + Name + " " + SymbolTableEntry + " " + LocalVariableCount;
        }
    }

    public abstract class StatementNode : ISyntaxNode
    {
        public abstract IEnumerable<ISyntaxNode> GetChildren();
        public abstract void Accept(ISyntaxNodeVisitor visitor);
    }

    public class BlockStatementNode : StatementNode
    {
        public List<DeclarationNode> Declarations { get; private set; }
        public List<StatementNode> Statements { get; private set; }

        public BlockStatementNode()
        {
            Declarations = new List<DeclarationNode>();
            Statements = new List<StatementNode>();
        }

        public override IEnumerable<ISyntaxNode> GetChildren()
        {
            return Declarations.Cast<ISyntaxNode>().Concat(Statements);
        }
        
        public override void Accept(ISyntaxNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class AssignmentStatementNode : StatementNode
    {
        public IdentifierNode Variable { get; private set; }
        public ExpressionNode Expression { get; private set; }
        public SymbolTableEntry SymbolTableEntry { get; set; }

        public AssignmentStatementNode(IdentifierNode variable, ExpressionNode expression)
        {
            Variable = variable;
            Expression = expression;
        }

        public override IEnumerable<ISyntaxNode> GetChildren()
        {
            return new ISyntaxNode[] {Variable, Expression};
        }

        public override void Accept(ISyntaxNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return "Assignment: " + SymbolTableEntry;
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

        public override void Accept(ISyntaxNodeVisitor visitor)
        {
            visitor.Visit(this);
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

        public override void Accept(ISyntaxNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class IfStatementNode : StatementNode
    {
        public ExpressionNode Condition { get; private set; }
        public StatementNode ThenBody { get; private set; }
        public StatementNode ElseBody { get; private set; }

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

        public override void Accept(ISyntaxNodeVisitor visitor)
        {
            visitor.Visit(this);
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

        public override void Accept(ISyntaxNodeVisitor visitor)
        {
            visitor.Visit(this);
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

        public override void Accept(ISyntaxNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public interface IPrintItemNode : ISyntaxNode
    {
    }

    public class InputStatementNode : StatementNode
    {
        public List<VariableReferenceNode> TargetVariables { get; private set; }

        public InputStatementNode()
        {
            TargetVariables = new List<VariableReferenceNode>();
        }

        public override IEnumerable<ISyntaxNode> GetChildren()
        {
            return TargetVariables;
        }

        public override void Accept(ISyntaxNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public abstract class ExpressionNode : ISyntaxNode, IPrintItemNode
    {
        public abstract IEnumerable<ISyntaxNode> GetChildren();
        public abstract void Accept(ISyntaxNodeVisitor visitor);
    }

    public enum Operator
    {
        Equal,
        NotEqual,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
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

        public override void Accept(ISyntaxNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return "BinaryExpression: " + Operator;
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

        public override void Accept(ISyntaxNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return "UnaryExpression: " + Operator;
        }
    }

    public class FunctionCallNode : ExpressionNode
    {
        public IdentifierNode Name { get; private set; }
        public List<ExpressionNode> Arguments { get; private set; }
        public SymbolTableEntry SymbolTableEntry { get; set; }

        public FunctionCallNode(IdentifierNode name)
        {
            Name = name;
            Arguments = new List<ExpressionNode>();
        }

        public override IEnumerable<ISyntaxNode> GetChildren()
        {
            return new ISyntaxNode[] {Name}.Concat(Arguments);
        }

        public override void Accept(ISyntaxNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return "FunctionCall: " + SymbolTableEntry;
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

        public override void Accept(ISyntaxNodeVisitor visitor)
        {
            visitor.Visit(this);
        }
    }

    public class VariableReferenceNode : ExpressionNode
    {
        public IdentifierNode Variable { get; private set; }
        public SymbolTableEntry SymbolTableEntry { get; set; }

        public VariableReferenceNode(IdentifierNode variable)
        {
            Variable = variable;
        }

        public override IEnumerable<ISyntaxNode> GetChildren()
        {
            return new[] {Variable};
        }

        public override void Accept(ISyntaxNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return "VariableReference: " + SymbolTableEntry;
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

        public void Accept(ISyntaxNodeVisitor visitor)
        {
            visitor.Visit(this);
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

        public void Accept(ISyntaxNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return "Identifier: " + Name;
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

        public void Accept(ISyntaxNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return "Integer: " + Value;
        }
    }

    public class StringNode : ISyntaxNode, IPrintItemNode
    {
        public string Value { get; private set; }

        public StringNode(string value)
        {
            Value = value;
        }

        public void Accept(ISyntaxNodeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public IEnumerable<ISyntaxNode> GetChildren()
        {
            return new ISyntaxNode[0];
        }
    }
}
