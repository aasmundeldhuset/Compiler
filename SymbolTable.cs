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
        public int StackOffset { get; private set; }

        public SymbolTableEntry(string label, SymbolTableEntryType type, int stackOffset)
        {
            Label = label;
            Type = type;
            StackOffset = stackOffset;
        }

        public override string ToString()
        {
            return Label + " " + Type + " " + StackOffset;
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
                int stackOffset = -4;
                FindSymbolsRecursively(function.Body, ref stackOffset);
                _scopeStack.PopScope();
                function.LocalVariablesSize = -4 - stackOffset;
            }
        }
        
        private void AddSymbol(string key, SymbolTableEntry entry)
        {
            _symbols.Add(key, entry);
        }

        private void FindSymbolsRecursively(ISyntaxNode node, ref int stackOffset)
        {
            if (node is BlockStatementNode)
            {
                var block = (BlockStatementNode) node;
                _scopeStack.PushAnonymousScope();
                var declaredIdentifiers = block.Declarations.SelectMany(d => d.Variables).ToList();
                EnterIdentifiers(declaredIdentifiers, SymbolTableEntryType.Variable, ref stackOffset);
                foreach (var statement in block.Statements)
                {
                    FindSymbolsRecursively(statement, ref stackOffset);
                }
                _scopeStack.PopScope();
            }
            else if (node is IdentifierNode)
            {
                var identifier = (IdentifierNode) node;
                var symbolTableEntry = FindDeclaration(identifier.Name);
                if (symbolTableEntry == null)
                    throw new Exception(string.Format("Variable {0} is not declared", identifier.Name));
                identifier.SymbolTableEntry = symbolTableEntry;
            }
            else
            {
                foreach (var child in node.GetChildren())
                {
                    FindSymbolsRecursively(child, ref stackOffset);
                }
            }
        }

        private void EnterIdentifiers(IList<IdentifierNode> identifiers, SymbolTableEntryType type, ref int stackOffset)
        {
            int offset;
            if (type == SymbolTableEntryType.Parameter)
                offset = 8 + (identifiers.Count - 1) * 4;
            else if (type == SymbolTableEntryType.Variable)
                offset = stackOffset;
            else
                throw new ArgumentException("type must be Parameter or Variable", "type");

            foreach (var identifier in identifiers)
            {
                string scopeString = _scopeStack.CreateScopeString(identifier.Name);
                var entry = new SymbolTableEntry(identifier.Name, type, offset);
                AddSymbol(scopeString, entry);
                offset -= 4;
            }

            if (type == SymbolTableEntryType.Variable)
                stackOffset = offset;
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
    }
}
