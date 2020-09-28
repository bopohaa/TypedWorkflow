using System;
using System.Collections.Generic;
using System.Text;

namespace TypedWorkflow
{
    [AttributeUsage(AttributeTargets.Constructor)]
    public class TwConstructorAttribute : Attribute
    {
        public readonly bool _resolvePerInstance;
        public bool ResolvePerInstance => _resolvePerInstance;

        public TwConstructorAttribute(bool resolve_per_instance = false) => _resolvePerInstance = resolve_per_instance;
    }
}
