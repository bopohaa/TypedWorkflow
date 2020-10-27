using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using static TypedWorkflow.Common.ExpressionFactory;

namespace TypedWorkflow.Common
{
    internal class TwComponentFactory
    {
        private readonly ExpressionFactory.Activator _activator;
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

        private void ResolveConstructorArgs()
        {
            for (var i = 0; i < _constructorArgTypes.Length; ++i)
            {
                _constructorArgs[i] = _resolver.Resolve(_constructorArgTypes[i]);
            }
        }
    }
}
