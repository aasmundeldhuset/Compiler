using System.Collections.Generic;
using System.Linq;

namespace Compiler
{
    public abstract class SyntaxNode
    {
        public abstract IEnumerable<SyntaxNode> GetChildren();
    }

    public class ProgramNode : SyntaxNode
    {
        public List<FunctionNode> Functions { get; private set; }

        public ProgramNode()
        {
            Functions = new List<FunctionNode>();
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            return Functions;
        }
    }

    public class FunctionNode : SyntaxNode
    {
        public IdentifierNode Name { get; private set; }
        public List<IdentifierNode> Parameters { get; private set; }
        public BlockNode Body { get; set; }

        public FunctionNode(IdentifierNode name)
        {
            Name = name;
            Parameters = new List<IdentifierNode>();
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            return new SyntaxNode[] {Name}.Concat(Parameters).Concat(new[] {Body});
        }
    }

    public abstract class StatementNode : SyntaxNode
    {
        
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

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            return Declarations.Cast<SyntaxNode>().Concat(Statements);
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


        public override IEnumerable<SyntaxNode> GetChildren()
        {
            return new SyntaxNode[] {Variable, Expression};
        }
    }

    public class ReturnStatementNode : StatementNode
    {
        
    }

    public class NullStatementNode : StatementNode
    {
        
    }

    public class IfStatementNode : StatementNode
    {
        
    }

    public class WhileStatementNode : StatementNode
    {
        
    }

    public class PrintStatementNode : StatementNode
    {
        public List<PrintItemNode> Items { get; set; }
    }

    public class PrintItemNode
    {
        
    }

    public abstract class ExpressionNode : SyntaxNode
    {
        
    }

    public class BinaryExpressionNode : ExpressionNode
    {
        
    }

    public class UnaryExpressionNode : ExpressionNode
    {
        
    }

    public class DeclarationNode : SyntaxNode
    {
        public List<IdentifierNode> Variables { get; private set; }

        public DeclarationNode()
        {
            Variables = new List<IdentifierNode>();
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            return Variables;
        }
    }

    public class IdentifierNode : SyntaxNode
    {
        public string Name { get; set; }

        public IdentifierNode(string name)
        {
            Name = name;
        }
    }

    public class IntegerNode : SyntaxNode
    {
        public int Value { get; set; }
    }

    public class StringNode : SyntaxNode
    {
        public string Value { get; set; }
    }
}
