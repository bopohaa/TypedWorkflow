using System;
using System.Collections.Generic;
using System.Text;

namespace TypedWorkflowTests.Common
{
    public interface IStringBuilder
    {
        IStringBuilder AppendLine(string value);
    }

    public class StringBuilderService : IStringBuilder
    {
        private readonly StringBuilder _sb = new StringBuilder();

        public IStringBuilder AppendLine(string value)
        {
            lock (_sb)
                _sb.AppendLine(value);
            return this;
        }

        public override string ToString()
            => _sb.ToString();
    }
}
