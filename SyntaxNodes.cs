using System.Collections.Generic;

namespace Compiler
{
    public abstract class SyntaxNode
    {
    }

    public class ProgramNode : SyntaxNode
    {
        public List<FunctionNode> Functions { get; set; }

        public ProgramNode()
        {
            Functions = new List<FunctionNode>();
        }
    }

    public class FunctionNode : SyntaxNode
    {
        public string Name { get; set; }
        public List<VariableNode> Parameters { get; set; }

        public FunctionNode(string name)
        {
            Name = name;
            Parameters = new List<VariableNode>();
        }
    }

    public abstract class StatementNode
    {
        
    }

    public class BlockNode : StatementNode
    {
        
    }

    public class AssignmentStatementNode : StatementNode
    {
        
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

    public class DeclarationNode
    {
        
    }

    public class VariableNode : SyntaxNode
    {
        public string Name { get; set; }

        public VariableNode(string name)
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
