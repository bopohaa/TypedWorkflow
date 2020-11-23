using System;
using System.Collections.Generic;
using System.Reflection;

using static TypedWorkflow.Common.ExpressionFactory;

namespace TypedWorkflow.Common
{
    internal class TwComponentFactory
    {
        private readonly ExpressionFactory.Activator _activator;
        private readonly Deactivator _deactivator;
        private readonly Type[] _constructorArgTypes;

        public TwComponentFactory(IEnumerable<MethodInfo> initialization, ConstructorInfo constructor, IResolver resolver)
        {
            foreach (var init in initialization)
                Initialization(init, resolver);

            if (constructor is null)
                return;

            (_activator, _constructorArgTypes) = GetActivator(constructor);
            _deactivator = GetDeactivator(constructor.DeclaringType);

        }

        public object CreateInstance(ISimpleResolver resolver, object[] activate_params_buffer = null)
        {
            if (_activator is null)
                return null;

            var args = activate_params_buffer ?? CreateActivateParamsBuffer();
            ResolveArgs(_constructorArgTypes, resolver, args);
            return _activator(args);
        }

        public void TryDisposeInstance(object instance)
        {
            if (_deactivator != null && instance != null)
                _deactivator(instance);
        }

        public object[] CreateActivateParamsBuffer() => new object[_constructorArgTypes.Length];

        private static void Initialization(MethodInfo initialization, IResolver resolver)
        {
            var (init, argTypes) = GetInitializer(initialization);
            var args = new object[argTypes.Length];
            ResolveArgs(argTypes, resolver, args);
            init(args);
        }

        private static void ResolveArgs(Type[] arg_types, ISimpleResolver resolver, object[] result)
        {
            for (var i = 0; i < result.Length; ++i)
                result[i] = resolver.Resolve(arg_types[i]);
        }
    }
}
