using System;

namespace Jellyfin.Plugin.JellyfinEnhanced.Services
{
    /// <summary>
    /// Evaluates the module gates of js/component-gates.json: flag names joined
    /// with <c>&amp;&amp;</c>, <c>||</c> and <c>!</c>, with parentheses
    /// (e.g. <c>JellyseerrEnabled &amp;&amp; (JellyseerrShowSimilar || JellyseerrShowRecommended)</c>).
    /// A malformed gate counts as true, so a mistake ships a module rather than
    /// dropping one a page needs.
    /// </summary>
    internal static class GateExpression
    {
        internal static bool Evaluate(string expression, Func<string, bool> flag)
        {
            try
            {
                var parser = new Parser(expression, flag);
                var value = parser.Or();
                parser.SkipSpace();
                return parser.AtEnd ? value : true;
            }
            catch (FormatException)
            {
                return true;
            }
        }

        private sealed class Parser
        {
            private readonly string _text;
            private readonly Func<string, bool> _flag;
            private int _pos;

            public Parser(string text, Func<string, bool> flag)
            {
                _text = text;
                _flag = flag;
            }

            public bool AtEnd => _pos >= _text.Length;

            public void SkipSpace()
            {
                while (!AtEnd && char.IsWhiteSpace(_text[_pos]))
                {
                    _pos++;
                }
            }

            private bool Take(string token)
            {
                SkipSpace();
                if (string.CompareOrdinal(_text, _pos, token, 0, token.Length) == 0)
                {
                    _pos += token.Length;
                    return true;
                }

                return false;
            }

            public bool Or()
            {
                var value = And();
                while (Take("||"))
                {
                    var right = And();
                    value = value || right;
                }

                return value;
            }

            private bool And()
            {
                var value = Not();
                while (Take("&&"))
                {
                    var right = Not();
                    value = value && right;
                }

                return value;
            }

            private bool Not()
            {
                if (Take("!"))
                {
                    return !Not();
                }

                if (Take("("))
                {
                    var value = Or();
                    if (!Take(")"))
                    {
                        throw new FormatException("missing )");
                    }

                    return value;
                }

                SkipSpace();
                var start = _pos;
                while (!AtEnd && (char.IsLetterOrDigit(_text[_pos]) || _text[_pos] == '_'))
                {
                    _pos++;
                }

                if (_pos == start)
                {
                    throw new FormatException("flag name expected");
                }

                return _flag(_text[start.._pos]);
            }
        }
    }
}
