using System;
using System.Collections.Generic;
using System.Text;

namespace TypedWorkflow
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class TwConstraintAttribute : Attribute
    {
        private readonly Type _constraint;
        public Type Constraint => _constraint;

        public bool HasNone { get; set; }

        public TwConstraintAttribute(Type constraint)
        {
            _constraint = constraint;
        }
    }
}