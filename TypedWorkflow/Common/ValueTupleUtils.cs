using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TypedWorkflow.Common
{
    internal static class ValueTupleUtils
    {
        private static Type[] _valueTupleTypes = new[] { typeof(ValueTuple<>), typeof(ValueTuple<,>), typeof(ValueTuple<,,>), typeof(ValueTuple<,,,>), typeof(ValueTuple<,,,,>), typeof(ValueTuple<,,,,,>), typeof(ValueTuple<,,,,,,>), typeof(ValueTuple<,,,,,,,>) };

        public static bool IsValueTuple(Type type) =>
            type.IsGenericType ? _valueTupleTypes.Contains(type.GetGenericTypeDefinition()) : false;

        public static bool TryUnwrap(Type type, out Type[] tuple_types)
        {
            if (!IsValueTuple(type))
            {
                tuple_types = null;
                return false;
            }

            tuple_types = type.GenericTypeArguments;
            return true;
        }

        public static bool TryUnwrap(Type type, out Type[] tuple_types, out FieldInfo[] tuple_fields)
        {
            if(!TryUnwrap(type, out tuple_types))
            {
                tuple_fields = null;
                return false;
            }

            tuple_fields = type.GetFields();
            return true;
        }
    }
}
