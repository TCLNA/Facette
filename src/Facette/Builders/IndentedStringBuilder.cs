using System.Text;

namespace Facette.Generator.Builders
{
    /// <summary>
    /// A thin wrapper around <see cref="StringBuilder"/> that automatically manages
    /// indentation for generated code output.
    /// </summary>
    internal sealed class IndentedStringBuilder
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private int _indentLevel;
        private const string IndentUnit = "    ";

        public IndentedStringBuilder Indent()
        {
            _indentLevel++;
            return this;
        }

        public IndentedStringBuilder Unindent()
        {
            if (_indentLevel > 0)
                _indentLevel--;
            return this;
        }

        public IndentedStringBuilder AppendLine(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                _sb.AppendLine();
            }
            else
            {
                for (int i = 0; i < _indentLevel; i++)
                    _sb.Append(IndentUnit);
                _sb.AppendLine(line);
            }
            return this;
        }

        public IndentedStringBuilder AppendLine()
        {
            _sb.AppendLine();
            return this;
        }

        public IndentedStringBuilder Append(string text)
        {
            _sb.Append(text);
            return this;
        }

        public override string ToString()
        {
            return _sb.ToString();
        }
    }
}
