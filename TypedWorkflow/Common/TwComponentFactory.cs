using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace TypedWorkflow.Common
{
    internal class TwComponentFactory
    {
        delegate object Activator(params object[] args);
        delegate void Deactivator(object instance);

        private readonly Activator _activator;
        private readonly Deactivator _deactivator;
        private readonly bool _resolvePerInstance;
        private readonly IResolver _resolver;
        private readonly Type[] _constructorArgTypes;
        private readonly object[] _constructorArgs;

        public TwComponentFactory(ConstructorInfo constructor, IResolver resolver)
        {
            var constructorAttr = constructor.GetCustomAttribute<TwConstructorAttribute>();

            _resolver = resolver;
            _resolvePerInstance = constructorAttr?.ResolvePerInstance ?? false;
            (_activator, _constructorArgTypes) = GetActivator(constructor);
            _deactivator = GetDeactivator(constructor.DeclaringType);
            _constructorArgs = new object[_constructorArgTypes.Length];
            if (!_resolvePerInstance)
                ResolveConstructorArgs();
        }

        public object CreateInstance()
        {
            if (_resolvePerInstance)
                ResolveConstructorArgs();
            return _activator(_constructorArgs);
        }

        public void TryDisposeInstance(object instance)
        {
            if (_deactivator != null && instance != null)
                _deactivator(instance);
        }

        private static Deactivator GetDeactivator(Type type)
        {
            if (!typeof(IDisposable).IsAssignableFrom(type))
                return null;
            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var convExp = Expression.Convert(instanceParam, typeof(IDisposable));
            var callExp = Expression.Call(convExp, typeof(IDisposable).GetMethod(nameof(IDisposable.Dispose)));
            var lambda = Expression.Lambda(typeof(Deactivator), callExp, instanceParam);
            return (Deactivator)lambda.Compile();
        }

        private static (Activator, Type[]) GetActivator(ConstructorInfo ctor)
        {
            Type type = ctor.DeclaringType;
            ParameterInfo[] paramsInfo = ctor.GetParameters();
            ParameterExpression param =
                Expression.Parameter(typeof(object[]), "args");

            Expression[] argsExp =
                new Expression[paramsInfo.Length];
            var constructorArgs = paramsInfo.Select(p => p.ParameterType).ToArray();

            for (int i = 0; i < constructorArgs.Length; i++)
            {
                Expression index = Expression.Constant(i);
                Type paramType = constructorArgs[i];

                Expression paramAccessorExp =
                    Expression.ArrayIndex(param, index);

                Expression paramCastExp =
                    Expression.Convert(paramAccessorExp, paramType);

                argsExp[i] = paramCastExp;
            }
            NewExpression newExp = Expression.New(ctor, argsExp);
            LambdaExpression lambda =
                Expression.Lambda(typeof(Activator), newExp, param);

            return ((Activator)lambda.Compile(), constructorArgs);
        }

        private void ResolveConstructorArgs()
        {
            for (var i = 0; i < _constructorArgTypes.Length; ++i)
            {
                _constructorArgs[i] = _resolver.Resolve(_constructorArgTypes[i]);
            }
        }
    }
}
