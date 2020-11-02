using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace TypedWorkflow.Common
{
    internal class TwContainer : ITwContainer
    {
        private readonly ObjectPool<TwContext> _factory;
        private readonly Func<Task<object[]>, object, object[]> _complete;

        public TwContainer(ObjectPool<TwContext> factory)
        {
            _factory = factory;
            _complete = Complete;
        }

        public ValueTask Run()
        {
            var res = Run(Array.Empty<object>());
            return res.IsCompleted ? new ValueTask() : new ValueTask(res.AsTask());
        }

        protected ValueTask<object[]> Run(object[] initial_imports)
        {
            var context = _factory.Allocate();
            var res = context.RunAsync(initial_imports);
            if (res.IsCompleted)
            {
                _complete(null, context);
                return res;
            }
            return new ValueTask<object[]>(res.AsTask().ContinueWith(_complete, context));
        }

        private object[] Complete(Task<object[]> complete, object state)
        {
            var context = (TwContext)state;
            _factory.Free(context);
            return complete?.Result;
        }

    }

    internal class TwContainer<T> : TwContainer, ITwContainer<T>
    {
        private readonly Func<T, object[]> _getInitialImports;
        protected Func<T, object[]> GetInitialImports => _getInitialImports;

        public TwContainer(ObjectPool<TwContext> factory, FieldInfo[] initial_tuple_fields) : base(factory)
        {
            if (typeof(T) == typeof(Option.Void))
                _getInitialImports = e => Array.Empty<object>();
            else if (initial_tuple_fields == null)
                _getInitialImports = GetInitialImportsImpl;
            else
                _getInitialImports = v => GetInitialImportsImpl(v, initial_tuple_fields);
        }

        public ValueTask Run(T initial_imports)
        {
            var res = Run(GetInitialImports(initial_imports));
            return res.IsCompleted ? new ValueTask() : new ValueTask(res.AsTask());
        }

        private object[] GetInitialImportsImpl(T initial_imports)
            => new[] { (object)initial_imports };

        private object[] GetInitialImportsImpl(T initial_imports, FieldInfo[] initial_tuple_fields)
        {
            var initialImports = new object[initial_tuple_fields.Length];
            for (int i = 0; i < initial_tuple_fields.Length; ++i)
                initialImports[i] = initial_tuple_fields[i].GetValue(initial_imports);
            return initialImports;
        }
    }

    internal class TwContainer<T, Tr> : TwContainer<T>, ITwContainer<T, Tr>
    {
        private readonly ConstructorInfo _resultTupleConstructor;
        private readonly Func<Task<object[]>, Tr> _success;

        public TwContainer(ObjectPool<TwContext> factory, FieldInfo[] initial_tuple_fields, Type[] result_tuple_types) : base(factory, initial_tuple_fields)
        {
            _success = t => GetResult(t.Result);

            if (result_tuple_types != null)
            {
                var genericType = Type.GetType("System.ValueTuple`" + result_tuple_types.Length);
                _resultTupleConstructor = genericType.MakeGenericType(result_tuple_types).GetConstructor(result_tuple_types);
            }
        }

        public new ValueTask<Tr> Run(T initial_imports)
        {
            var res = Run(GetInitialImports(initial_imports));
            if (res.IsCompleted)
                return new ValueTask<Tr>(GetResult(res.Result));
            var t = res.AsTask().ContinueWith(_success, TaskContinuationOptions.OnlyOnRanToCompletion);
            return new ValueTask<Tr>(t);
        }

        private Tr GetResult(object[] result)
        {
            if (_resultTupleConstructor != null)
                return (Tr)_resultTupleConstructor.Invoke(result);

            if (result.Length > 1)
                throw new InvalidCastException("Many result values");

            return (Tr)result[0];
        }
    }

}