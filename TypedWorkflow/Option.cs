using System;
using System.Collections.Generic;
using System.Text;
using TypedWorkflow.Common;

namespace TypedWorkflow
{
    public abstract class Option
    {
        public struct Void { }
        public bool HasValue { get; private set; }
        public object Value { get; private set; }

        internal Option() { }
        internal Option(object value) { HasValue = true; Value = value; }

        public static Option<T> Create<T>(T value) => new Option<T>(value);
    }

    public sealed class Option<T> : Option
    {
        public Option() : base() { }
        public Option(T value) : base(value) { }

        public T Model => (T)base.Value;

        private static Option<T> _none;
        public static Option<T> None => _none ?? (_none = new Option<T>());

        private static Option<T> _someDefault;
        public static Option<T> SomeDefault => _someDefault ?? (_someDefault = new Option<T>(default));

    }

}
