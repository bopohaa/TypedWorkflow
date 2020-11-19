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
        private readonly Type[] _constructorArgTypes;
        private readonly object[] _constructorArgs;
        private bool _canResolve;

        public TwComponentFactory(ConstructorInfo constructor)
        {
            var constructorAttr = constructor.GetCustomAttribute<TwConstructorAttribute>();

            _resolvePerInstance = constructorAttr?.ResolvePerInstance ?? false;
            (_activator, _constructorArgTypes) = GetActivator(constructor);
            _deactivator = GetDeactivator(constructor.DeclaringType);
            _constructorArgs = new object[_constructorArgTypes.Length];
            _canResolve = true;
        }

        public object CreateInstance(ISimpleResolver resolver)
        {
            if (_canResolve)
            {
                for (var i = 0; i < _constructorArgTypes.Length; ++i)
                    _constructorArgs[i] = resolver.Resolve(_constructorArgTypes[i]);

                _canResolve = _resolvePerInstance;
            }
            return _activator(_constructorArgs);
        }

        public void TryDisposeInstance(object instance)
        {
            if (_deactivator != null && instance != null)
                _deactivator(instance);
        }
    }
}
