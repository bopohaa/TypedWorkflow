using System;

namespace TypedWorkflow.Common
{
    internal class TwContextMeta
    {
        public readonly int ComponentsCount;
        public readonly IEntrypoint[] Entrypoints;
        public readonly (TwComponentFactory, int[])[] ScopedInstances;
        public readonly int[][] ExportIndex;
        public readonly ExpressionFactory.Activator[] ExportOptionNoneFactories;
        public readonly ExpressionFactory.Activator[] ExportOptionSomeFactories;
        public readonly int[][] ImportIndex;
        public readonly TwConstraintIndex[][] ConstraintIndex;
        public readonly int[] ExecuteList;
        public readonly int ExportCnt;
        public readonly int InitialEntrypointIdx;
        public readonly int ResultEntrypointIdx;

        public TwContextMeta(int components_count, IEntrypoint[] entrypoints, (TwComponentFactory, int[])[] scoped_instances, int[][] exportIndex, int[][] importIndex, TwConstraintIndex[][] constraintIndex, int[] executeList, int export_cnt, int initial_entrypoint_idx, int result_entrypoint_idx, ExpressionFactory.Activator[] export_none_factories, ExpressionFactory.Activator[] export_some_factories)
        {
            ComponentsCount = components_count;
            Entrypoints = entrypoints;
            ScopedInstances = scoped_instances;
            ExportIndex = exportIndex;
            ImportIndex = importIndex;
            ConstraintIndex = constraintIndex;
            ExecuteList = executeList;
            ExportCnt = export_cnt;
            InitialEntrypointIdx = initial_entrypoint_idx;
            ResultEntrypointIdx = result_entrypoint_idx;
            ExportOptionNoneFactories = export_none_factories;
            ExportOptionSomeFactories = export_some_factories;
        }
    }
}
