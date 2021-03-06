﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace TypedWorkflow.Common
{
    internal static class ExpressionFactory
    {
        public delegate void Initializer(params object[] args);
        public delegate object Activator(params object[] args);
        public delegate void Deactivator(object instance);
        public delegate object Call(object instance, params object[] args);
        public delegate T GetProperty<T>(object instance);

        public static (Initializer, Type[]) GetInitializer(MethodInfo init)
        {
            var (param, argsExp) = GetParamsExp(init.GetParameters(), out var initArgs);
            var callExp = Expression.Call(init, argsExp);
            LambdaExpression lambda =
                Expression.Lambda(typeof(Initializer), callExp, param);

            return ((Initializer)lambda.Compile(), initArgs);
        }

        public static (Activator, Type[]) GetActivator(ConstructorInfo ctor)
        {
            var (param, argsExp) = GetParamsExp(ctor.GetParameters(), out var constructorArgs);
            NewExpression newExp = Expression.New(ctor, argsExp);
            LambdaExpression lambda =
                Expression.Lambda(typeof(Activator), newExp, param);

            return ((Activator)lambda.Compile(), constructorArgs);
        }

        public static Deactivator GetDeactivator(Type type)
        {
            if (!typeof(IDisposable).IsAssignableFrom(type))
                return null;
            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var convExp = Expression.Convert(instanceParam, typeof(IDisposable));
            var callExp = Expression.Call(convExp, typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose)));
            var lambda = Expression.Lambda(typeof(Deactivator), callExp, instanceParam);
            return (Deactivator)lambda.Compile();
        }

        public static (Type[] types, bool[] is_option) GetExpandedTypes(Type[] types)
        {
            var typesRes = new List<Type>(types.Length);
            var isOptionRes = new List<bool>(types.Length);
            for (var i = 0; i < types.Length; i++)
            {
                var e = types[i];
                var isOption = e.IsGenericType && e.GetGenericTypeDefinition() == typeof(Option<>);
                isOptionRes.Add(isOption);
                typesRes.Add(isOption ? e.GetGenericArguments().Single() : e);

            }
            return (typesRes.ToArray(), isOptionRes.ToArray());
        }

        public static Call GetMethodExecutor(MethodInfo method)
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

            var callExp = method.IsStatic ? Expression.Call(method, argsExp) : Expression.Call(Expression.Convert(instanceParam, type), method, argsExp);
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

        public static GetProperty<T> GetPropertyExecutor<T>(PropertyInfo prop)
        {
            var instanceParam =
                Expression.Parameter(typeof(object), "instance");
            var callExp = Expression.Property(Expression.Convert(instanceParam, prop.DeclaringType), prop);
            var callExpCast = Expression.Convert(callExp, typeof(T));
            var lambda = Expression.Lambda(typeof(GetProperty<T>), callExpCast, instanceParam);
            var compiled = (GetProperty<T>)lambda.Compile();

            return compiled;
        }

        private static (ParameterExpression, Expression[]) GetParamsExp(ParameterInfo[] params_info, out Type[] method_args)
        {
            var param = Expression.Parameter(typeof(object[]), "args");
            var argsExp = new Expression[params_info.Length];
            method_args = params_info.Select(p => p.ParameterType).ToArray();

            for (int i = 0; i < method_args.Length; i++)
            {
                Expression index = Expression.Constant(i);
                Type paramType = method_args[i];

                Expression paramAccessorExp =
                    Expression.ArrayIndex(param, index);

                Expression paramCastExp =
                    Expression.Convert(paramAccessorExp, paramType);

                argsExp[i] = paramCastExp;
            }

            return (param, argsExp);
        }

    }
}
