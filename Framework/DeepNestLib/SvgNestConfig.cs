namespace DeepNestLib
{
    public class SvgNestConfig
    {
        public PlacementTypeEnum placementType = PlacementTypeEnum.box;
        public double curveTolerance = 0.72;
        public double scale = 25;
        public double clipperScale = 10000000;
        public bool exploreConcave = false;
        public int mutationRate = 10;
        public int populationSize = 100;
        public int rotations = 4;
        public double spacing = 0;
        public double sheetSpacing = 0;
        public bool useHoles = false;
        public double timeRatio = 0.5;
        public bool mergeLines = false;
        public bool simplify;

        #region port features (don't exist in the original DeepNest project)
        public bool clipByHull = false;
        public bool clipByRects = true; //clip by AABB + MinRect
        #endregion

        /// <summary>
        /// 启用晶格排样（周期性排列）
        /// </summary>
        public bool UseLatticePlacement { get; set; } = true;

        /// <summary>
        /// 触发晶格排样的最小同质形状数量
        /// </summary>
        public int MinLatticeCount { get; set; } = 5;

        /// <summary>
        /// 晶格优化最大迭代次数
        /// </summary>
        public int LatticeMaxIterations { get; set; } = 150;

        /// <summary>
        /// 轮次放置最大迭代次数
        /// </summary>
        public int PlacePartsGenerationSteps { get; set; } = 4;
    }
}
