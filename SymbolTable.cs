using System;
using System.Collections.Generic;
using System.Linq;

namespace Compiler
{
    public enum SymbolTableEntryType
    {
        Variable,
        Parameter,
        Function,
    }

    public class SymbolTableEntry
    {
        public string Label { get; private set; }
        public SymbolTableEntryType Type { get; private set; }
        public int Index { get; private set; }

        public SymbolTableEntry(string label, SymbolTableEntryType type, int index)
        {
            Label = label;
            Type = type;
            Index = index;
        }

        public override string ToString()
        {
            return Label + " " + Type + " " + Index;
        }
    }

    public class SymbolTable
    {
        private readonly Dictionary<string, SymbolTableEntry> _symbols = new Dictionary<string, SymbolTableEntry>();
        private readonly ScopeStack _scopeStack = new ScopeStack();

        public void FindSymbols(ProgramNode program)
        {
            // We want functions to be callable independently of their declaration order, so we do a first pass to find all function names
            foreach (var function in program.Functions)
            {
                var entry = new SymbolTableEntry(function.Name.Name, SymbolTableEntryType.Function, 0);
                AddSymbol(function.Name.Name, entry);
            }
            foreach (var function in program.Functions)
            {
                _scopeStack.PushScope(function.Name.Name);
                int unused = 0;
                EnterIdentifiers(function.Parameters, SymbolTableEntryType.Parameter, ref unused);
                int localVariableIndex = 0;
                FindSymbolsRecursively(function.Body, ref localVariableIndex);
                _scopeStack.PopScope();
                function.LocalVariableCount = localVariableIndex;
            }
        }
        
        private void FindSymbolsRecursively(ISyntaxNode node, ref int localVariableIndex)
        {
            if (node is BlockStatementNode)
            {
                var block = (BlockStatementNode) node;
                _scopeStack.PushAnonymousScope();
                var declaredIdentifiers = block.Declarations.SelectMany(d => d.Variables).ToList();
                EnterIdentifiers(declaredIdentifiers, SymbolTableEntryType.Variable, ref localVariableIndex);
                foreach (var statement in block.Statements)
                {
                    FindSymbolsRecursively(statement, ref localVariableIndex);
                }
                _scopeStack.PopScope();
            }
            else if (node is VariableReferenceNode)
            {
                var reference = (VariableReferenceNode) node;
                var symbolTableEntry = FindDeclaration(reference.Variable.Name);
                if (symbolTableEntry == null)
                    throw new Exception(string.Format("Variable {0} is not declared", reference.Variable.Name));
                reference.SymbolTableEntry = symbolTableEntry;
            }
            else if (node is AssignmentStatementNode)
            {
                var assign = (AssignmentStatementNode) node;
                var symbolTableEntry = FindDeclaration(assign.Variable.Name);
                if (symbolTableEntry == null)
                    throw new Exception(string.Format("Variable {0} is not declared", assign.Variable.Name));
                assign.SymbolTableEntry = symbolTableEntry;
                FindSymbolsRecursively(assign.Expression, ref localVariableIndex);
            }
            else if (node is FunctionCallNode)
            {
                var call = (FunctionCallNode) node;
                var symbolTableEntry = FindDeclaration(call.Name.Name);
                if (symbolTableEntry == null)
                    throw new Exception(string.Format("Function {0} is not declared", call.Name.Name));
                call.SymbolTableEntry = symbolTableEntry;
                foreach (var argument in call.Arguments)
                {
                    FindSymbolsRecursively(argument, ref localVariableIndex);
                }
            }
            else
            {
                foreach (var child in node.GetChildren())
                {
                    FindSymbolsRecursively(child, ref localVariableIndex);
                }
            }
        }

        private void EnterIdentifiers(IEnumerable<IdentifierNode> identifiers, SymbolTableEntryType type, ref int localVariableIndex)
        {
            int index;
            if (type == SymbolTableEntryType.Parameter)
                index = 0;
            else if (type == SymbolTableEntryType.Variable)
                index = localVariableIndex;
            else
                throw new ArgumentException("type must be Parameter or Variable", "type");

            foreach (var identifier in identifiers)
            {
                string scopeString = _scopeStack.CreateScopeString(identifier.Name);
                var entry = new SymbolTableEntry(identifier.Name, type, index);
                AddSymbol(scopeString, entry);
                ++index;
            }

            if (type == SymbolTableEntryType.Variable)
                localVariableIndex = index;
        }

        private SymbolTableEntry FindDeclaration(string name)
        {
            foreach (string scopeString in _scopeStack.CreateScopeStrings(name))
            {
                if (_symbols.ContainsKey(scopeString))
                    return _symbols[scopeString];
            }
            return null;
        }

        private void AddSymbol(string key, SymbolTableEntry entry)
        {
            _symbols.Add(key, entry);
        }
    }
}
