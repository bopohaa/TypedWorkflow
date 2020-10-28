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
        private readonly Option[] _exportOptionInstances;
        private readonly bool[] _exportInstanceIsNone;
        private readonly object[][] _inputArgs;
        private readonly object[] _output;
        private object[] _instances;

        public TwContext(TwContextMeta meta)
        {
            _meta = meta;
            _exportInstances = new object[_meta.ExportCnt];
            _exportInstanceIsNone = new bool[_meta.ExportCnt];
            _exportOptionInstances = new Option[_meta.ExportCnt];
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
                    var outputArgs = _meta.ExportIndex[idx];
                    var inputArgs = _meta.ImportIndex[idx];
                    var input = _meta.InitialEntrypointIdx == idx ? initial_imports : _inputArgs[idx];
                    int loaded = CheckConstraints(idx) ? LoadInput(entry, inputArgs, input) : -1;
                    if (loaded < inputArgs.Length)
                    {
                        SaveOutputAsNone(outputArgs);
                        continue;
                    }

                    if (entry.IsAsync)
                        await entry.ExecuteAsync(instance, input, _output);
                    else
                        entry.Execute(instance, input, _output);

                    SaveOutput(entry, outputArgs, _output);
                }
            }
            finally
            {
                DisposeScopedInstances();
            }
        }

        private bool CheckConstraints(int entrypoint_idx)
        {
            var constraints = _meta.ConstraintIndex[entrypoint_idx];
            
            for (var i = 0; i < constraints.Length; i++)
            {
                var isNone = _exportInstanceIsNone[constraints[i].Index];
                if (constraints[i].HasNone != isNone)
                    return false;
            }

            return true;
        }

        private int LoadInput(IEntrypoint entry, int[] inputArgs, object[] input)
        {
            int i = 0;
            for (; i < inputArgs.Length; ++i)
            {
                var idx = inputArgs[i];
                var d = _exportInstances[idx];
                if (entry.ImportIsOption[i])
                {
                    var opt = _exportOptionInstances[idx];
                    if (opt == null)
                    {
                        opt = (Option)(_exportInstanceIsNone[idx] ? _meta.ExportOptionNoneFactories[idx]() : _meta.ExportOptionNoneFactories[idx](d));
                        _exportOptionInstances[idx] = opt;
                    }
                    d = opt;
                }
                else if (_exportInstanceIsNone[idx])
                    break;
                input[i] = d;
            }

            return i;
        }

        private void SaveOutputAsNone(int[] outputArgs)
        {
            for (var i = 0; i < outputArgs.Length; i++)
            {
                var j = outputArgs[i];
                _exportInstances[j] = null;
                _exportInstanceIsNone[j] = true;
            }
        }

        private void SaveOutput(IEntrypoint entry, int[] outputArgs, object[] output)
        {
            for (int i = 0; i < outputArgs.Length; ++i)
            {
                var idx = outputArgs[i];
                if (entry.ExportIsOption[i])
                {
                    var e = (Option)output[i];
                    _exportInstances[idx] = e.Value;
                    _exportInstanceIsNone[idx] = !e.HasValue;
                    _exportOptionInstances[idx] = e;
                }
                else
                {
                    _exportInstances[idx] = output[i];
                    _exportInstanceIsNone[idx] = false;
                }
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