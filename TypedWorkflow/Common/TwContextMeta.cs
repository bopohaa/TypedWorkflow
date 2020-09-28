namespace TypedWorkflow.Common
{
    internal struct TwContextMeta
    {
        public readonly IEntrypoint[] Entrypoints;
        public readonly object[] Instances;
        public readonly (TwComponentFactory, int[])[] ScopedInstances;
        public readonly int[][] ExportIndex;
        public readonly int[][] ImportIndex;
        public readonly int[] ExecuteList;
        public readonly int ExportCnt;
        public readonly int InitialEntrypointIdx;
        public readonly int ResultEntrypointIdx;

        public TwContextMeta(IEntrypoint[] entrypoints, object[] instances, (TwComponentFactory, int[])[] scoped_instances, int[][] exportIndex, int[][] importIndex, int[] executeList, int export_cnt, int initial_entrypoint_idx, int result_entrypoint_idx)
        {
            Entrypoints = entrypoints;
            Instances = instances;
            ScopedInstances = scoped_instances;
            ExportIndex = exportIndex;
            ImportIndex = importIndex;
            ExecuteList = executeList;
            ExportCnt = export_cnt;
            InitialEntrypointIdx = initial_entrypoint_idx;
            ResultEntrypointIdx = result_entrypoint_idx;
        }

        public bool IsEmpty => Entrypoints == null;
    }
}
