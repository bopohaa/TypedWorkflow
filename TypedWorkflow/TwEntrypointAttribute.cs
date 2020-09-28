using System;
using System.Collections.Generic;
using System.Text;

namespace TypedWorkflow
{
    [AttributeUsage(AttributeTargets.Method)]
    public class TwEntrypointAttribute : Attribute
    {
        public TwEntrypointPriorityEnum Priority { get; private set; }
        public TwEntrypointAttribute(TwEntrypointPriorityEnum priority = TwEntrypointPriorityEnum.Medium)
        {
            Priority = priority;
        }

    }
}
