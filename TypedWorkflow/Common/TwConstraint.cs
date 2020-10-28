using System;
using System.Collections.Generic;
using System.Text;

namespace TypedWorkflow.Common
{
    internal struct TwConstraint
    {
        public readonly Type Constraint;
        public readonly bool HasNone;

        public TwConstraint(Type constraint, bool has_none)
        {
            Constraint = constraint;
            HasNone = has_none;
        }
    }

    internal struct TwConstraintIndex
    {
        public readonly int Index;
        public readonly bool HasNone;

        public TwConstraintIndex(int index, bool has_none)
        {
            Index = index;
            HasNone = has_none;
        }

    }
}
