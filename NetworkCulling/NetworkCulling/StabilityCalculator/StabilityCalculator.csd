namespace StabilityManager
{
    public class StabilityCalculatorEx
    {
        private static Dictionary<Vector2i, FallQueue> queueFall = new Dictionary<Vector2i, FallQueue>();
        public static Queue<Vector3i> queueStability = new Queue<Vector3i>();
        public static WorldBase bWorld = null;
        public static ChannelCalculatorEx channelCalculator = null;
        public static bool running = false;
        public static HashSet<Vector3i> stab0Positions = new HashSet<Vector3i>();

        public static void Init(WorldBase _world);

        public static void PlacedBlock(Vector3i _pos, bool _bForceFullStabe = false);
        public static void RemovedBlock(Vector3i _pos);

        private static void EnqueueFallingBlocks(IList<Vector3i> _blocks);
        private static void EnqueueFallingBlock(Vector3i _pos);

        private static int GetDelay();
        private static void TryStartThread();
        private static void CleanupFallQueue();
        private static void CheckFallQueue();
        private static void CheckStabilityQueue();

        private class FallQueue {
            public Vector2i cPos { get; set; }
            public Vector3i bPos { get; set; }
            public int blocksHandled { get; set; }
            public List<Vector3i> queue { get; set; }
            public float timeCreated { get; set; }
            public float timeLastActivity { get; set; }
            public FallQueue(Vector2i cPos);
            public void AddBlock(Vector3i _pos);
            public void CheckChunkQueue();
        }

        private static List<Vector3i> 
            CalcPhysicsStabilityToFall(Vector3i _pos, int maxBlocksToCheck, out float calculatedStability);
    }
}
