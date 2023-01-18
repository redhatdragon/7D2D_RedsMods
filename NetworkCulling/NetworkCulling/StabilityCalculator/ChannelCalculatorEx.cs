using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StabilityManager
{
    public class ChannelCalculatorEx
    {
        private readonly WorldBase world;
        [ThreadStatic]
        private static HashSet<Vector3i> List;
        [ThreadStatic]
        private static List<Vector3i> List2;

        public ChannelCalculatorEx(WorldBase _world) => this.world = _world;

        private static HashSet<Vector3i> list
        {
            get
            {
                if (List == null)
                    List = new HashSet<Vector3i>();
                return List;
            }
        }

        private static List<Vector3i> list2
        {
            get
            {
                if (List2 == null)
                    List2 = new List<Vector3i>();
                return List2;
            }
        }

        public void BlockRemovedAt(Vector3i _pos, HashSet<Vector3i> _stab0Positions)
        {
            BlockValue blockValue = this.world.GetBlock(_pos);
            Block block = blockValue.Block;
            //TODO: is this conditional a bug with the &&?
            //if (!blockValue.isair && block.blockMaterial.IsLiquid || block.StabilityIgnore)
            if (!blockValue.isair || block.blockMaterial.IsLiquid || block.StabilityIgnore)
                return;
            list.Clear();
            list2.Clear();
            this.CalcChangedPositionsFromRemove(_pos, list2, _stab0Positions);
            IChunk _chunk = (IChunk)null;
            for (int i = 0; i < list2.Count; ++i)
            {
                Vector3i vector3i = list2[i];
                if (this.world.GetChunkFromWorldPos(vector3i, ref _chunk))
                {
                    int blockXz1 = World.toBlockXZ(vector3i.x);
                    int blockY = World.toBlockY(vector3i.y);
                    int blockXz2 = World.toBlockXZ(vector3i.z);
                    int stability = (int)_chunk.GetStability(blockXz1, blockY, blockXz2);
                    if (stability > 1)
                        this.ChangeStability(vector3i, stability, null, _stab0Positions, _chunk);
                }
            }
        }

        public void BlockPlacedAt(Vector3i _pos, bool _isForceFullStab) // _isForceFullStab = flying block
        {
            int stabilityStart = 15;
            if (!_isForceFullStab)
            {
                --_pos.y;
                if (GameManager.Instance.World.GetBlock(_pos).type != BlockValue.Air.type)
                    stabilityStart = (int)this.world.GetStability(_pos);
                else stabilityStart = 0;
                ++_pos.y;
            }

            if (stabilityStart == 15) // lets just look from here
            {
                List<Vector3i> vector3iList = new List<Vector3i>();
                Block block1;
                BlockValue block2;
                while (!(block2 = this.world.GetBlock(_pos)).isair && !(block1 = block2.Block).blockMaterial.IsLiquid && !block1.StabilityIgnore)
                {
                    if (!block1.StabilitySupport)
                    {
                        this.world.SetStability(_pos, (byte)1);
                        break;
                    }
                    this.world.SetStability(_pos, (byte)15);
                    vector3iList.Add(_pos);
                    ++_pos.y;
                }
                for (int index = vector3iList.Count - 1; index >= 0; --index)
                    this.ChangeStability(vector3iList[index], 15, null);
            }
            else
            {
                bool _bFromDownwards;
                int maxStabilityAround = this.getMaxStabilityAround(_pos, out _bFromDownwards);
                int _stab = _bFromDownwards ? maxStabilityAround : maxStabilityAround - 1;
                BlockValue block3 = this.world.GetBlock(_pos);
                Block block4 = block3.Block;
                if (block3.isair || block4.blockMaterial.IsLiquid || block4.StabilityIgnore)
                    return;
                if (_stab > 1 && !block4.StabilitySupport)
                    _stab = 1;
                this.world.SetStability(_pos, _stab < 0 ? (byte)0 : (byte)_stab);
                this.ChangeStability(_pos, _stab, null);
            }
        }

        private int getMaxStabilityAround(Vector3i _pos, out bool _bFromDownwards)
        {
            _bFromDownwards = false;
            int maxStabilityAround = 0;
            int num = 0;
            Vector3i[] allDirections = Vector3i.AllDirections;
            for (int index = 0; index < allDirections.Length; ++index)
            {
                Vector3i _pos1 = _pos + allDirections[index];
                int stability = (int)this.world.GetStability(_pos1);
                if (allDirections[index].y == -1)
                    num = stability;
                if (stability > maxStabilityAround && this.world.GetBlock(_pos1).Block.StabilitySupport)
                    maxStabilityAround = stability;
            }
            _bFromDownwards = maxStabilityAround == num;
            return maxStabilityAround;
        }

        private void CalcChangedPositionsFromRemove(
          Vector3i _pos,
          List<Vector3i> _neighbors,
          HashSet<Vector3i> _stab0Positions,
          IChunk chunk = null)
        {
            int stability1 = (int)this.world.GetStability(_pos);
            this.world.SetStability(_pos, (byte)0);
            _stab0Positions.Add(_pos);
            foreach (Vector3i allDirection in Vector3i.AllDirections)
            {
                Vector3i vector3i = _pos + allDirection;
                if (this.world.GetChunkFromWorldPos(vector3i, ref chunk))
                {
                    Vector3i block1 = World.toBlock(vector3i);
                    BlockValue blockNoDamage = chunk.GetBlockNoDamage(block1.x, block1.y, block1.z);
                    if (!blockNoDamage.isair)
                    {
                        Block block2 = blockNoDamage.Block;
                        if (!block2.blockMaterial.IsLiquid && !block2.StabilityIgnore)
                        {
                            int stability2 = (int)chunk.GetStability(block1.x, block1.y, block1.z);
                            if (stability2 != 1 || block2.StabilitySupport)
                            {
                                if (stability2 == stability1 - 1 || allDirection.y == 1 && stability2 == stability1)
                                    this.CalcChangedPositionsFromRemove(vector3i, _neighbors, _stab0Positions, chunk);
                                else if (stability2 >= stability1)
                                    _neighbors.Add(vector3i);
                            }
                        }
                    }
                }
            }
        }

        private void ChangeStability(
          Vector3i _pos,
          int _stab,
          List<Vector3i> _changedPositions,
          HashSet<Vector3i> _stab0Positions = null,
          IChunk chunk = null)
        {
            foreach (Vector3i allDirection in Vector3i.AllDirections)
            {
                Vector3i vector3i = _pos + allDirection;
                if (this.world.GetChunkFromWorldPos(vector3i, ref chunk))
                {
                    Vector3i block1 = World.toBlock(vector3i);
                    BlockValue blockNoDamage = chunk.GetBlockNoDamage(block1.x, block1.y, block1.z);
                    if (!blockNoDamage.isair)
                    {
                        Block block2 = blockNoDamage.Block;
                        if (!block2.blockMaterial.IsLiquid && !block2.StabilityIgnore)
                        {
                            int num = _stab - 1;
                            if ((int)chunk.GetStability(block1.x, block1.y, block1.z) < num)
                            {
                                if (!block2.StabilitySupport && num > 1)
                                    num = 1;
                                if (_stab0Positions != null)
                                {
                                    if (num == 0)
                                        _stab0Positions.Add(vector3i);
                                    else
                                        _stab0Positions.Remove(vector3i);
                                }
                                _changedPositions?.Add(vector3i);
                                chunk.SetStability(block1.x, block1.y, block1.z, (byte)num);
                                this.ChangeStability(vector3i, num, _changedPositions, _stab0Positions, chunk);
                            }
                        }
                    }
                }
            }
        }
    }
}
