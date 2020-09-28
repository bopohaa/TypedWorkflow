using System;
using System.Reflection;
using System.Threading.Tasks;

namespace TypedWorkflow.Common
{
    internal class TwContainer : ITwContainer
    {
        private readonly ObjectPool<TwContext> _factory;
        private readonly Action<bool, TwContext> _complete;

        public TwContainer(ObjectPool<TwContext> factory)
        {
            _factory = factory;
            _complete = Complete;
        }

        public ValueTask Run()
        {
            var context = AllocateContext();
            return context.Run(_complete);
        }

        protected TwContext AllocateContext()
        {
            return _factory.Allocate();
        }

        protected void FreeContext(TwContext context)
        {
            _factory.Free(context);
        }

        private void Complete(bool complete, TwContext context)
        {
            FreeContext(context);
        }

    }

    internal class TwContainer<T> : TwContainer, ITwContainer<T>
    {
        private readonly object[] _initialImports;
        private readonly FieldInfo[] _initialTupleFields;
        private readonly Action<bool, TwContext> _complete;

        public TwContainer(ObjectPool<TwContext> factory, FieldInfo[] initial_tuple_fields) : base(factory)
        {
            _initialTupleFields = initial_tuple_fields;
            _complete = Complete;

            _initialImports = new object[_initialTupleFields?.Length ?? 1];
        }

        public ValueTask Run(T initial_imports)
        {
            var context = AllocateContext();
            return context.Run(_complete, SetInitialImports(initial_imports));
        }

        protected object[] SetInitialImports(T initial_imports)
        {
            if (_initialTupleFields != null)
                for (int i = 0; i < _initialTupleFields.Length; ++i)
                    _initialImports[i] = _initialTupleFields[i].GetValue(initial_imports);
            else
                _initialImports[0] = initial_imports;

            return _initialImports;
        }

        private void Complete(bool complete, TwContext context)
        {
            FreeContext(context);
        }
    }

    internal class TwContainer<T, Tr> : TwContainer<T>, ITwContainer<T, Tr>
    {
        private readonly ConstructorInfo _resultTupleConstructor;
        private readonly Func<bool, TwContext, object[], Tr> _complete;

        public TwContainer(ObjectPool<TwContext> factory, FieldInfo[] initial_tuple_fields, Type[] result_tuple_types) : base(factory, initial_tuple_fields)
        {
            _complete = Complete;

            if (result_tuple_types != null)
            {
                var genericType = Type.GetType("System.ValueTuple`" + result_tuple_types.Length);
                _resultTupleConstructor = genericType.MakeGenericType(result_tuple_types).GetConstructor(result_tuple_types);
            }
        }

        public new ValueTask<Tr> Run(T initial_imports)
        {
            var context = AllocateContext();
            return context.Run(_complete, SetInitialImports(initial_imports));
        }

        private Tr Complete(bool complete, TwContext context, object[] result)
        {
            FreeContext(context);

            if (!complete)
                return default(Tr);

            if (_resultTupleConstructor != null)
                return (Tr)_resultTupleConstructor.Invoke(result);

            if (result.Length > 1)
                throw new InvalidCastException("Many result values");

            return (Tr)result[0];
        }
    }

}