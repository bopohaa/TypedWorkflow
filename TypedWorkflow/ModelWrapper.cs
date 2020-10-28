using System;
using System.Collections.Generic;
using System.Text;

namespace TypedWorkflow
{
    public abstract class ModelWrapper<T>
    {
        private readonly T _value;
        public T Value => _value;

        public ModelWrapper(T value) => _value = value;
    }
}
