using System;
using System.Collections.Generic;
using System.Text;
using TypedWorkflow;
using TypedWorkflowTests.Common;

namespace TypedWorkflowTests.Components
{
    public class CustomConstructorComponent
    {
        private const string LINE = "#CustomConstructorComponent.Run#";
        private readonly IStringBuilder _sb;

        public CustomConstructorComponent()
        {
            _sb = new StringBuilderService();
        }

        [TwConstructor]
        public CustomConstructorComponent(IStringBuilder sb)
        {
            _sb = sb;
        }

        [TwEntrypoint]
        public void Run()
        {
            _sb.AppendLine(LINE);
        }

        public static bool Assert(string builder_result, int iteration_cnt)
            => builder_result.Split(LINE).Length - 1 == iteration_cnt;
    }
}
