﻿using System;
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

        private readonly TwConstraint[] _constraints;
        public TwConstraint[] Constraints => _constraints;

        private readonly bool _isSingleton;
        public bool IsSingleton => _isSingleton;

        private ComponentEntrypoint(bool is_singleton, Type instance_type, ExpressionFactory.Call method, ExpressionFactory.GetProperty<object> task_result_prop, Type[] imports, Type[] exports, TwConstraint[] constraints, FieldInfo[] tuple_fields, TwEntrypointPriorityEnum priority, bool is_async)
        {
            _isSingleton = is_singleton;
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

            _constraints = constraints;
        }

        public void Execute(object instance, object[] args, object[] output)
        {
            if (args.Length != Import.Length)
                throw new InvalidOperationException("Invalid args count");

            var result = _method(instance, args);

            ExpandResult(result, output);
        }

        public async Task ExecuteAsync(object instance, object[] args, object[] output)
        {
            if (args.Length != Import.Length)
                throw new InvalidOperationException("Invalid args count");

            var task = ((Task)_method(instance, args));

            await task;
            
            ExpandResult(_taskResultPoperty?.Invoke(task), output);
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

            var constraints = GetConstraints(method, type);

            return new ComponentEntrypoint(method.IsStatic, type, callMethod, getTaskResultProperty, imports, exports, constraints, tupleFields, priority, isAsync);
        }

        private static TwConstraint[] GetConstraints(MethodInfo method, Type type)
        {
            Func<MemberInfo, IEnumerable<TwConstraint>> getConstraints = e => e.GetCustomAttributes<TwConstraintAttribute>(true).Select(a =>new TwConstraint(a.Constraint, a.HasNone));
            var entrypointConstraints =  getConstraints(method);
            var classConstraints = getConstraints(type);
            var constraints = classConstraints.Concat(entrypointConstraints).ToArray();
            return constraints;
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
