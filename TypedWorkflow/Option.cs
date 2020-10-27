using System;
using System.Collections.Generic;
using System.Text;
using TypedWorkflow.Common;

namespace TypedWorkflow
{
    public abstract class Option
    {
        public bool HasValue { get; private set; }
        public object Value { get; private set; }

        internal Option() { }
        internal Option(object value) { HasValue = true; Value = value; }

        public Option<T> Create<T>(T value) => new Option<T>(value);

        //public static Option<Model1<T>> GetNoneModel1<T>() => Option<Model1<T>>.None;
        //public static Option<Model1<T>> CreateModel1<T>(T value) => new Option<Model1<T>>(new Model1<T>(value));

        //public static Option<Model2<T>> GetNoneModel2<T>() => Option<Model2<T>>.None;
        //public static Option<Model2<T>> CreateModel2<T>(T value) => new Option<Model2<T>>(new Model2<T>(value));

        //public static Option<Model3<T>> GetNoneModel3<T>() => Option<Model3<T>>.None;
        //public static Option<Model3<T>> CreateModel3<T>(T value) => new Option<Model3<T>>(new Model3<T>(value));

        //public static Option<Model4<T>> GetNoneModel4<T>() => Option<Model4<T>>.None;
        //public static Option<Model4<T>> CreateModel4<T>(T value) => new Option<Model4<T>>(new Model4<T>(value));
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
