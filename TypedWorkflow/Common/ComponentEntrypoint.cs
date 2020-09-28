using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace TypedWorkflow.Common
{
    internal class ComponentEntrypoint : IEntrypoint
    {
        delegate object Call(object instance, params object[] args);
        delegate T GetProperty<T>(object instance);

        private readonly Call _method;
        private readonly GetProperty<object> _taskResultPoperty;
        private readonly FieldInfo[] _tupleFields;
        public Type[] Export { get; }

        public Type[] Import { get; }

        public Type InstanceType { get; }

        public TwEntrypointPriorityEnum Priority { get; }

        public bool IsAsync { get; }

        private ComponentEntrypoint(Type instance_type, Call method, GetProperty<object> task_result_prop, Type[] imports, Type[] exports, FieldInfo[] tuple_fields, TwEntrypointPriorityEnum priority, bool is_async)
        {
            _method = method;
            _taskResultPoperty = task_result_prop;
            _tupleFields = tuple_fields;
            InstanceType = instance_type;

            Import = imports;
            Export = exports;
            Priority = priority;
            IsAsync = is_async;
        }

        public int Execute(object instance, object[] args, object[] output)
        {
            if (args.Length != Import.Length)
                throw new InvalidOperationException("Invalid args count");

            var result = _method(instance, args);

            return GetResult(result, output);
        }

        public Task<int> ExecuteAsync(object instance, object[] args, object[] output)
        {
            if (args.Length != Import.Length)
                throw new InvalidOperationException("Invalid args count");

            return ((Task)_method(instance, args))
                .ContinueWith(t => GetResult(_taskResultPoperty?.Invoke(t), output), TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        private static Type[] _valueTupleTypes = new[] { typeof(ValueTuple<>), typeof(ValueTuple<,>), typeof(ValueTuple<,,>), typeof(ValueTuple<,,,>), typeof(ValueTuple<,,,,>), typeof(ValueTuple<,,,,,>), typeof(ValueTuple<,,,,,,>), typeof(ValueTuple<,,,,,,,>) };
        public static ComponentEntrypoint Create(MethodInfo method, TwEntrypointPriorityEnum priority)
        {
            var type = method.DeclaringType;
            var param = method.GetParameters();
            var export = method.ReturnType == typeof(void) ? null : method.ReturnType;
            var isAsync = export != null && (export == typeof(Task) || export.IsSubclassOf(typeof(Task)));
            export = isAsync ? export?.GenericTypeArguments?.FirstOrDefault() : export;

            var imports = param.Select(p => p.ParameterType).ToArray();
            var isMultipleRet = export?.IsGenericType ?? false ? _valueTupleTypes.Contains(export.GetGenericTypeDefinition()) : false;
            var exports = isMultipleRet ? export.GenericTypeArguments : export != null ? new[] { export } : Array.Empty<Type>();

            var getTaskResultProperty = isAsync && export != null ? GetPropertyExecutor<object>(typeof(Task<>).MakeGenericType(method.ReturnType.GenericTypeArguments.Single()).GetProperty("Result")) : null;
            var tupleFields = isMultipleRet ? export.GetFields() : null;
            var callMethod = GetMethodExecutor(method);

            return new ComponentEntrypoint(type, callMethod, getTaskResultProperty, imports, exports, tupleFields, priority, isAsync);
        }

        private int GetResult(object result, object[] output)
        {
            if (Export.Length == 0)
                return 0;
            if (Export.Length == 1 && _tupleFields == null)
            {
                output[0] = result;
                return 1;
            }

            for (int i = 0; i < _tupleFields.Length; ++i)
                output[i] = _tupleFields[i].GetValue(result);

            return _tupleFields.Length;
        }


        static Call GetMethodExecutor(MethodInfo method)
        {
            Type type = method.DeclaringType;
            ParameterInfo[] paramsInfo = method.GetParameters();
            ParameterExpression instanceParam =
                Expression.Parameter(typeof(object), "instance");
            ParameterExpression param =
                Expression.Parameter(typeof(object[]), "args");

            Expression[] argsExp =
                new Expression[paramsInfo.Length];

            for (int i = 0; i < paramsInfo.Length; i++)
            {
                Expression index = Expression.Constant(i);
                Type paramType = paramsInfo[i].ParameterType;

                Expression paramAccessorExp =
                    Expression.ArrayIndex(param, index);

                Expression paramCastExp =
                    Expression.Convert(paramAccessorExp, paramType);

                argsExp[i] = paramCastExp;
            }
            LambdaExpression lambda;
            var callExp = Expression.Call(Expression.Convert(instanceParam, type), method, argsExp);
            if (method.ReturnType == typeof(void))
            {
                var callExpCast = Expression.Block(callExp, Expression.Constant(null));
                lambda = Expression.Lambda(typeof(Call), callExpCast, instanceParam, param);
            }
            else
            {
                var callExpCast = Expression.Convert(callExp, typeof(object));
                lambda = Expression.Lambda(typeof(Call), callExpCast, instanceParam, param);
            }

            var compiled = (Call)lambda.Compile();

            return compiled;
        }

        static GetProperty<T> GetPropertyExecutor<T>(PropertyInfo prop)
        {
            var instanceParam =
                Expression.Parameter(typeof(object), "instance");
            var callExp = Expression.Property(Expression.Convert(instanceParam, prop.DeclaringType), prop);
            var callExpCast = Expression.Convert(callExp, typeof(T));
            var lambda = Expression.Lambda(typeof(GetProperty<T>), callExpCast, instanceParam);
            var compiled = (GetProperty<T>)lambda.Compile();

            return compiled;
        }
    }
}
