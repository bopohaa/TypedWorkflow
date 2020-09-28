using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace TypedWorkflow.Common
{
    internal class TwContext
    {
        private readonly TwContextMeta _meta;
        private readonly object[] _exportInstances;
        private readonly object[][] _inputArgs;
        private readonly object[] _output;
        private object[] _instances;

        public TwContext(TwContextMeta meta)
        {
            _meta = meta;
            _exportInstances = new object[_meta.ExportCnt];
            _inputArgs = new object[_meta.Entrypoints.Length][];
            _output = new object[256];
            for (var i = 0; i < _meta.ImportIndex.Length; ++i)
                _inputArgs[i] = new object[_meta.ImportIndex[i].Length];
            _instances = new object[meta.Instances.Length];
            Array.Copy(meta.Instances, _instances, _instances.Length);
        }

        public async ValueTask Run(Action<bool, TwContext> complete, params object[] initial_imports)
        {
            try
            {
                var res = Run(initial_imports);
                if (!res.IsCompletedSuccessfully)
                    await res;
            }
            catch
            {
                complete(false, this);
                throw;
            }
            complete(true, this);
        }

        public async ValueTask<Tr> Run<Tr>(Func<bool, TwContext, object[], Tr> complete, params object[] initial_imports)
        {
            try
            {
                var res = Run(initial_imports);
                if (!res.IsCompletedSuccessfully)
                    await res;
            }
            catch
            {
                complete(false, this, null);
                throw;
            }
            return complete(true, this, (object[])_inputArgs[_meta.ResultEntrypointIdx].Clone());
        }

        private async ValueTask Run(object[] initial_imports)
        {
            try
            {
                CreateScopedInstances();
                foreach (var idx in _meta.ExecuteList)
                {
                    var entry = _meta.Entrypoints[idx];
                    var instance = _instances[idx];
                    var inputArgs = _meta.ImportIndex[idx];
                    var input = _meta.InitialEntrypointIdx == idx ? initial_imports : _inputArgs[idx];
                    for (int i = 0; i < inputArgs.Length; ++i)
                        input[i] = _exportInstances[inputArgs[i]];

                    var outputCnt = entry.IsAsync ?
                        await entry.ExecuteAsync(instance, input, _output) :
                        entry.Execute(instance, input, _output);

                    var outputArgs = _meta.ExportIndex[idx];
                    if (outputCnt != outputArgs.Length)
                        throw new InvalidOperationException("Invalid result");

                    for (int i = 0; i < outputCnt; ++i)
                        _exportInstances[outputArgs[i]] = _output[i];
                }
            }
            finally
            {
                DisposeScopedInstances();
            }
        }

        private void CreateScopedInstances()
        {
            foreach (var e in _meta.ScopedInstances)
            {
                var instance = e.Item1.CreateInstance();
                foreach (var idx in e.Item2)
                    _instances[idx] = instance;
            }
        }
        private void DisposeScopedInstances()
        {
            List<Exception> error = null;
            foreach (var e in _meta.ScopedInstances)
            {
                var instance = _instances[e.Item2[0]];
                foreach (var idx in e.Item2)
                    _instances[idx] = null;
                try { e.Item1.TryDisposeInstance(instance); } catch (Exception ex) { if (error == null) { error = new List<Exception>(); error.Add(ex); } }
            }
            if (error != null)
                ExceptionDispatchInfo.Capture(new AggregateException(error)).Throw();
        }

    }
}