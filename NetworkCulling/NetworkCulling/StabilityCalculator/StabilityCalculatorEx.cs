using System;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;

using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace StabilityManager
{
    public class StabilityCalculatorEx
    {
        public void CustomLog(string message,
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0) {
            // Do logging
        }

        private static Dictionary<Vector2i, FallQueue> queueFall = new Dictionary<Vector2i, FallQueue>();
        public static Queue<Vector3i> queueStability = new Queue<Vector3i>();
        public static WorldBase bWorld = null;
        public static ChannelCalculatorEx channelCalculator = null;
        public static bool running = false;
        public static HashSet<Vector3i> stab0Positions = new HashSet<Vector3i>();

        public static void Init(WorldBase _world) {
            bWorld = _world;
            //return;
            channelCalculator = new ChannelCalculatorEx(_world);
        }

        public static void PlacedBlock(Vector3i _pos, bool _bForceFullStabe = false)
        {
            return;
            channelCalculator.BlockPlacedAt(_pos, _bForceFullStabe);
            queueStability.Enqueue(_pos);
            TryStartThread();
        }

        public static void RemovedBlock(Vector3i _pos)
        {
            return;
            stab0Positions.Clear();
            channelCalculator.BlockRemovedAt(_pos, stab0Positions);
            World world = GameManager.Instance.World;
            foreach (Vector3i currentDirection in Vector3i.AllDirections)
            {
                IChunk owningChunk = null;
                Vector3i currentNeighborPos = _pos + currentDirection;
                if (!world.GetChunkFromWorldPos(currentNeighborPos, ref owningChunk))
                    continue;

                int blockX = World.toBlockXZ(currentNeighborPos.x);
                int blockY = World.toBlockY(currentNeighborPos.y);
                int blockZ = World.toBlockXZ(currentNeighborPos.z);

                int stability = (int)owningChunk.GetStability(blockX, blockY, blockZ);

                BlockValue blockNoDamage;
                if ((blockNoDamage = owningChunk.GetBlockNoDamage(blockX, blockY, blockZ)).isair)
                    stability = -1;
                else if (blockNoDamage.Block.blockMaterial.IsLiquid)
                    stability = -2;

                Log.Out($"stability: {stability}");

                if (stability < 0)
                    continue;

                if (stability == 0)
                {
                    if (!stab0Positions.Contains(currentNeighborPos))
                        EnqueueFallingBlock(currentNeighborPos);
                }
                //else if (stability < 15 && !queueStability.Count < 200)
                else if (stability < 15)
                    queueStability.Enqueue(currentNeighborPos);
            }
            foreach (Vector3i stab0Position in stab0Positions)
                EnqueueFallingBlock(stab0Position);

            TryStartThread();
        }
        private static void EnqueueFallingBlocks(IList<Vector3i> _blocks)
        {
            int count = _blocks.Count;
            for (int i = 0; i < count; ++i)
                EnqueueFallingBlock(_blocks[i]);
        }
        private static void EnqueueFallingBlock(Vector3i _pos)
        {
            //TODO: needs to be considered for rewrite, "AddBlock(_pos) is suspect when we are already
            //searching container by cPos.
            Vector2i cPos = World.toChunkXZ(_pos);
            if (!queueFall.ContainsKey(cPos))
                queueFall.Add(cPos, new FallQueue(cPos));
            queueFall[cPos].AddBlock(_pos);
        }
        private static int GetDelay()
        {
            // TODO: replace fps with Time.deltaTime which is the time per server tick from unity engine

            //float fps = GameManager.Instance.fps.Counter;
            float fps = Time.deltaTime;

            if (fps > 20)
                return 100;
            if (fps > 10)
                return 250;
            return 1000;
        }
        public static void TryStartThread()
        {
            //if (running)
                //return;
            running = true;

            //new Thread(() =>
            //{
                //Thread.CurrentThread.IsBackground = true;

                Log.Out($"StabilityCalculatorEx.Tick - New Thread");

                CheckFallQueue();
                CleanupFallQueue();
                CheckStabilityQueue();

                //Thread.Sleep(4000); // Slow down for testing

                Log.Out($"StabilityCalculatorEx.Tick - End Thread");

                running = false;

            //}).Start();
        }
        private static void CleanupFallQueue()
        {
            try
            {
                List<Vector2i> remove = new List<Vector2i>();
                foreach (KeyValuePair<Vector2i, FallQueue> kv in queueFall)
                {
                    if (kv.Value.queue.Count == 0)
                        remove.Add(kv.Key);
                }
                foreach (Vector2i p in remove)
                    queueFall.Remove(p);
                if (remove.Count > 0)
                    Log.Out($"RemoveFallQueue: {remove.Count}");
            }
            catch (Exception e) { Log.Out($"Cleanup: {e}"); }
        }
        private static void CheckFallQueue()
        {
            try
            {
                foreach (KeyValuePair<Vector2i, FallQueue> kv in queueFall)
                {
                    //int delay = GetDelay();
                    kv.Value.CheckChunkQueue();
                    //Thread.Sleep(delay);
                }
            }
            catch (Exception e) { Log.Out($"Fall: {e}"); }
        }
        private static void CheckStabilityQueue()
        {
            try
            {
                if (queueFall.Count > 0) // No cascade effect?
                    return;

                int max = Math.Min(queueStability.Count, 10);
                Log.Out($"CheckStabilityQueue: {max} max");
                for (int index = 0; index < max; index++)
                {
                    Vector3i pos = queueStability.Dequeue();
                    int delay = GetDelay();
                    List<Vector3i> blockPositionsToFall = CalcPhysicsStabilityToFall(pos, 20, out float _);

                    if (blockPositionsToFall != null && blockPositionsToFall.Count > 0)
                    {
                        Log.Out($"Stability: Add to fall {blockPositionsToFall.Count} blocks");
                        EnqueueFallingBlocks(blockPositionsToFall);
                        break;
                    }
                }
            }
            catch (Exception e) { Log.Out($"CheckStabilityQueue: Stability-> {e}"); }
        }
        private class FallQueue
        {
            public Vector2i cPos { get; set; }
            public Vector3i bPos { get; set; }
            public int blocksHandled { get; set; }
            public List<Vector3i> queue { get; set; }
            public float timeCreated { get; set; }
            public float timeLastActivity { get; set; }
            public FallQueue(Vector2i cPos)
            {
                this.cPos = cPos;
                this.blocksHandled = 0;
                this.queue = new List<Vector3i>();
                this.timeCreated = Time.time;
                this.timeLastActivity = Time.time;
            }
            public void AddBlock(Vector3i _pos)
            {
                Log.Out($"QueueFall.AddBlock {_pos}");
                this.queue.Add(_pos);
                this.timeLastActivity = Time.time;
            }
            public void CheckChunkQueue()
            {
                // Let blocks fall seperated
                //if (this.queue.Count < 10000)  //TODO: what?
                Log.Out($"CheckChunkQueue: running........");
                if(true)
                {
                    Log.Out($"Fall: Check at {this.cPos} with {this.queue.Count} in q (single)");
                    foreach (Vector3i pos in this.queue)
                    {
                        Log.Out($"Fall: Vanilla fall at {pos}");

                        GameManager.Instance.World.AddFallingBlock(pos);
                        blocksHandled++;
                    }
                    this.queue.Clear();
                    return;
                }
                // Send if 5s passed or 1s without adding new falling blocks
                else if (this.timeCreated + 5f < Time.time || this.timeLastActivity + 1f < Time.time)
                {
                    Log.Out($"Fall: Check at {this.cPos} with {this.queue.Count} in q (chunk send)");
                    Vector3i zero = Vector3i.zero;
                    World w = GameManager.Instance.World;

                    List<BlockChangeInfo> changedBlocks = new List<BlockChangeInfo>();
                    foreach (Vector3i pos in new Queue<Vector3i>(this.queue))
                    {
                        this.queue.Remove(pos);

                        if (blocksHandled == 0)
                            this.bPos = pos;

                        blocksHandled++;

                        if (zero.Equals(pos))
                        {
                            Log.Out($"Block falls at ZERO {pos}");
                            continue;
                        }

                        BlockValue block1 = w.GetBlock(pos.x, pos.y, pos.z);
                        if (!block1.isair)
                        {
                            changedBlocks.Add(new BlockChangeInfo(pos, BlockValue.Air, true));
                            DynamicMeshManager.ChunkChanged(pos, -1, block1.type);
                        }
                    }

                    Log.Out($"Fall: Change {changedBlocks.Count} blocks");

                    GameManager.Instance.ChangeBlocks(null, changedBlocks);

                    Chunk chunk = w.GetChunkFromWorldPos(this.cPos.x, 1, this.cPos.y) as Chunk;
                    GameManager.Instance.World.m_ChunkManager.ResendChunksToClients(new HashSetLong() { chunk.Key });

                    this.queue.Clear();
                    return;
                }
            }
        }
        private static List<Vector3i> CalcPhysicsStabilityToFall(Vector3i _pos, int maxBlocksToCheck, out float calculatedStability)
        {
            Log.Out($"CalcPhysicsStabilityToFall: {_pos}");

            List<Vector3i> blockPositionsToFall = (List<Vector3i>)null;
            calculatedStability = 0.0f;

            HashSet<Vector3i> unstablePositions = new HashSet<Vector3i>();
            unstablePositions.Add(_pos);

            Queue<Vector3i> positionsToCheck = new Queue<Vector3i>();
            positionsToCheck.Enqueue(_pos);

            Queue<Vector3i> uniqueUnstablePositions = new Queue<Vector3i>();

            World w = GameManager.Instance.World;

            //int force = 0;
            int mass = 0;
            IChunk chunk = null;
            for (int index = 0; index < maxBlocksToCheck; ++index)
            {
                Log.Out($"index: {index}");
                //int forceNew = force;
                int forceNew = 0;
                foreach (Vector3i pos in positionsToCheck)
                {
                    w.GetChunkFromWorldPos(pos, ref chunk);

                    BlockValue bv = chunk != null ? chunk.GetBlockNoDamage(World.toBlockXZ(pos.x), pos.y, World.toBlockXZ(pos.z)) : BlockValue.Air;

                    Block block = bv.Block;
                    mass += block.blockMaterial.Mass.Value;
                    //Log.Debug($"mass: {mass}");

                    foreach (Vector3i vector3i in Vector3i.AllDirectionsShuffled)
                    {
                        Vector3i pos2 = pos + vector3i;
                        if (chunk == null || chunk.X != World.toChunkXZ(pos2.x) || chunk.Z != World.toChunkXZ(pos2.z))
                            chunk = w.GetChunkFromWorldPos(pos2);

                        int blockXz1 = World.toBlockXZ(pos2.x);
                        int blockXz2 = World.toBlockXZ(pos2.z);
                        BlockValue bv2 = chunk != null ? chunk.GetBlockNoDamage(blockXz1, pos2.y, blockXz2) : BlockValue.Air;
                        int stability = bv2.isair || chunk == null ? 0 : (int)chunk.GetStability(blockXz1, pos2.y, blockXz2);
                        //Log.Debug($"stability: {stability}");
                        if (stability == 15)
                        {
                            int forceToOtherBlock = bv.GetForceToOtherBlock(bv2);
                            if (vector3i.y == -1)
                                forceNew = 100000;
                            else forceNew += forceToOtherBlock;

                            //force += forceToOtherBlock;
                        }
                        else if ((stability > 0 && bv2.Block.StabilitySupport || stability > 1) && unstablePositions.Add(pos2))
                        {
                            //Log.Debug($"add Unstable");
                            uniqueUnstablePositions.Enqueue(pos2);
                            if (vector3i.y == -1)
                                forceNew = 100000;
                            else forceNew += bv.GetForceToOtherBlock(bv2);
                        }
                    }
                }
                if (forceNew > 0)
                    calculatedStability = (float)(1.0 - (double)mass / (double)forceNew);

                Log.Out($"mass: {mass} forceNew: {forceNew} calculatedStability: {calculatedStability}");

                if (mass > forceNew)
                {
                    blockPositionsToFall = unstablePositions.Except<Vector3i>(uniqueUnstablePositions).ToList<Vector3i>();
                    Log.Out($"fall.Count: {blockPositionsToFall.Count}");
                    if (blockPositionsToFall.Count == 0)
                    {
                        calculatedStability = 1f;
                        break;
                    }
                    break;
                }

                if (uniqueUnstablePositions.Count == 0)
                    return blockPositionsToFall;

                positionsToCheck.Clear();
                Queue<Vector3i> unstablePositions2 = uniqueUnstablePositions;
                uniqueUnstablePositions = positionsToCheck;
                positionsToCheck = unstablePositions2;
                uniqueUnstablePositions.Clear();
            }
            return blockPositionsToFall;
        }
    }
}
