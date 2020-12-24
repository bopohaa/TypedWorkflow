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

        public ValueTask Run(CancellationToken cancellation = default(CancellationToken))
        {
            var res = Run(new object[] { cancellation });
            return res.IsCompletedSuccessfully ? new ValueTask() : new ValueTask(res.AsTask());
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
            if (complete is null)
                return null;

            if (complete.IsFaulted)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(complete.Exception).Throw();
            if (complete.IsCanceled)
                throw new TaskCanceledException(complete);

            return complete.Result;
        }

    }

    internal class TwContainer<T> : TwContainer, ITwContainer<T>
    {
        private readonly Func<T, CancellationToken, object[]> _getInitialImports;
        protected Func<T, CancellationToken, object[]> GetInitialImports => _getInitialImports;

        public TwContainer(ObjectPool<TwContext> factory, FieldInfo[] initial_tuple_fields) : base(factory)
        {
            if (typeof(T) == typeof(Option.Void))
                _getInitialImports = (e, c) => new object[] { c };
            else if (initial_tuple_fields == null)
                _getInitialImports = GetInitialImportsImpl;
            else
                _getInitialImports = (v, c) => GetInitialImportsImpl(v, initial_tuple_fields, c);
        }

        public ValueTask Run(T initial_imports, CancellationToken cancellation = default(CancellationToken))
        {
            var res = Run(GetInitialImports(initial_imports, cancellation));
            return res.IsCompletedSuccessfully ? new ValueTask() : new ValueTask(res.AsTask());
        }

        private object[] GetInitialImportsImpl(T initial_imports, CancellationToken cancellation)
            => new object[] { initial_imports, cancellation };

        private object[] GetInitialImportsImpl(T initial_imports, FieldInfo[] initial_tuple_fields, CancellationToken cancellation)
        {
            var initialImports = new object[initial_tuple_fields.Length + 1];

            for (int i = 0; i < initial_tuple_fields.Length; ++i)
                initialImports[i] = initial_tuple_fields[i].GetValue(initial_imports);

            initialImports[initial_tuple_fields.Length] = cancellation;

            return initialImports;
        }
    }

    internal class TwContainer<T, Tr> : TwContainer<T>, ITwContainer<T, Tr>
    {
        private readonly ConstructorInfo _resultTupleConstructor;
        private readonly Func<Task<object[]>, Tr> _success;
        private readonly ProactiveCache.ProCache<T, Tr> _cache;

        public TwContainer(ObjectPool<TwContext> factory, FieldInfo[] initial_tuple_fields, Type[] result_tuple_types, ProCacheFactory.Options<T, Tr> cache_options)
            : base(factory, initial_tuple_fields)
        {
            _success = Success;
            _cache = cache_options is null ? null : cache_options.CreateCache(RunImpl);

            if (result_tuple_types != null)
            {
                var genericType = Type.GetType("System.ValueTuple`" + result_tuple_types.Length);
                _resultTupleConstructor = genericType.MakeGenericType(result_tuple_types).GetConstructor(result_tuple_types);
            }
        }

        public new ValueTask<Tr> Run(T initial_imports, CancellationToken cancellation) =>
            _cache is null ? RunImpl(initial_imports, cancellation) : _cache.Get(initial_imports, cancellation);

        private ValueTask<Tr> RunImpl(T initial_imports, CancellationToken cancellation = default(CancellationToken))
        {
            var res = Run(GetInitialImports(initial_imports, cancellation));

            if (res.IsCompletedSuccessfully)
                return new ValueTask<Tr>(GetResult(res.Result));

            var t = res.AsTask().ContinueWith(_success, TaskContinuationOptions.OnlyOnRanToCompletion);

            return new ValueTask<Tr>(t);
        }

        private Tr Success(Task<object[]> successed_task)
            => GetResult(successed_task.Result);

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