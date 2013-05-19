using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Compiler
{
    public class ScopeStack
    {
        private readonly List<string> _scopes = new List<string>();
        private int _anonymousScopeCounter;

        public void PushScope(string name)
        {
            _scopes.Add(name);
        }

        public void PushAnonymousScope()
        {
            _scopes.Add(_anonymousScopeCounter.ToString());
            ++_anonymousScopeCounter;
        }

        public void PopScope()
        {
            _scopes.RemoveAt(_scopes.Count - 1);
        }

        public string CreateScopeString(string extraText)
        {
            return CreateScopeStrings(extraText).First();
        }

        public IEnumerable<string> CreateScopeStrings(string extraText)
        {
            var sb = new StringBuilder();
            foreach (var scope in _scopes)
            {
                sb.Append(scope);
                sb.Append('_');
            }
            for (int i = _scopes.Count - 1; i >= 0; --i)
            {
                sb.Append(extraText);
                yield return sb.ToString();
                // Remove extraText, the topmost underscore, and the topmost scope name
                sb.Length = sb.Length - extraText.Length - 1 - _scopes[i].Length;
            }
            yield return extraText;
        }
    }
}
