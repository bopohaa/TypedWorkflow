using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace TypedWorkflow.Common
{
    internal class ComponentEntrypoint : IEntrypoint
    {
        private readonly ExpressionFactory.Call _method;
        private readonly ExpressionFactory.GetProperty<object> _taskResultPoperty;
        private readonly FieldInfo[] _tupleFields;

        private readonly Type[] _export;
        public Type[] Export => _export;
        private readonly bool[] _exportIsOption;
        public bool[] ExportIsOption => _exportIsOption;

        private readonly Type[] _import;
        public Type[] Import => _import;
        private readonly bool[] _importIsOption;
        public bool[] ImportIsOption => _importIsOption;

        public Type InstanceType { get; }

        public TwEntrypointPriorityEnum Priority { get; }

        public bool IsAsync { get; }

        private ComponentEntrypoint(Type instance_type, ExpressionFactory.Call method, ExpressionFactory.GetProperty<object> task_result_prop, Type[] imports, Type[] exports, FieldInfo[] tuple_fields, TwEntrypointPriorityEnum priority, bool is_async)
        {
            _method = method;
            _taskResultPoperty = task_result_prop;
            _tupleFields = tuple_fields;
            InstanceType = instance_type;

            Priority = priority;
            IsAsync = is_async;

            var (export, exportIsOptions) = ExpressionFactory.GetExpandedTypes(exports);
            _export = export;
            _exportIsOption = exportIsOptions;

            var (import, importIsOptions) = ExpressionFactory.GetExpandedTypes(imports);
            _import = import;
            _importIsOption = importIsOptions;
        }

        public void Execute(object instance, object[] args, object[] output)
        {
            if (args.Length != Import.Length)
                throw new InvalidOperationException("Invalid args count");

            var result = _method(instance, args);

            ExpandResult(result, output);
        }

        public Task ExecuteAsync(object instance, object[] args, object[] output)
        {
            if (args.Length != Import.Length)
                throw new InvalidOperationException("Invalid args count");

            return ((Task)_method(instance, args))
                .ContinueWith(t => ExpandResult(_taskResultPoperty?.Invoke(t), output), TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        private static Type[] _valueTupleTypes = new[] { typeof(ValueTuple<>), typeof(ValueTuple<,>), typeof(ValueTuple<,,>), typeof(ValueTuple<,,,>), typeof(ValueTuple<,,,,>), typeof(ValueTuple<,,,,,>), typeof(ValueTuple<,,,,,,>), typeof(ValueTuple<,,,,,,,>) };
        public static ComponentEntrypoint Create(MethodInfo method, TwEntrypointPriorityEnum priority)
        {
            var type = method.DeclaringType;
            var param = method.GetParameters();
            var export = method.ReturnType == typeof(void) ? null : method.ReturnType;
            var isAsync = export != null && (export == typeof(Task) || export.IsSubclassOf(typeof(Task)));
            export = isAsync ? export.IsGenericType ? export.GenericTypeArguments.Single() : null : export;

            var imports = param.Select(p => p.ParameterType).ToArray();
            var isMultipleRet = export?.IsGenericType ?? false ? _valueTupleTypes.Contains(export.GetGenericTypeDefinition()) : false;
            var exports = isMultipleRet ? export.GenericTypeArguments : export != null ? new[] { export } : Array.Empty<Type>();

            var getTaskResultProperty = isAsync && export != null ? ExpressionFactory.GetPropertyExecutor<object>(typeof(Task<>).MakeGenericType(method.ReturnType.GenericTypeArguments.Single()).GetProperty("Result")) : null;
            var tupleFields = isMultipleRet ? export.GetFields() : null;
            var callMethod = ExpressionFactory.GetMethodExecutor(method);

            return new ComponentEntrypoint(type, callMethod, getTaskResultProperty, imports, exports, tupleFields, priority, isAsync);
        }

        private void ExpandResult(object result, object[] output)
        {
            if (_export.Length == 0)
                return;
            if (_tupleFields == null)
            {
                output[0] = result;
                return;
            }

            for (int i = 0; i < _tupleFields.Length; ++i)
                output[i] = _tupleFields[i].GetValue(result);
        }

    }
}
