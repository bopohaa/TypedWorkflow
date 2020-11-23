using TypedWorkflow;

using TypedWorkflowTests.Common;

namespace TypedWorkflowTests.Components
{
    public class DiCiComponent
    {
        private const string LINE = "#DiCiComponent.AppendText#";
        private IStringBuilder _sb;

        public DiCiComponent(IStringBuilder di_sb_service)
        {
            _sb = di_sb_service;
        }

        [TwEntrypoint]
        public void AppendText()
        {
            _sb.AppendLine(LINE);
        }

        public static bool Assert(string builder_result, int iteration_cnt)
            => builder_result.Split(LINE).Length - 1 == iteration_cnt;

    }
}
