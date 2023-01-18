using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModBase {
    /*unsafe struct AuxiliaryChunkData {
        struct AuxiliaryChunk {
            byte t;
            public const uint size = 1;
        }
        const uint MAX_CHUNK_BLOCKS = 1000;
        const uint SIZEOF_AUXILIARY_CHUNKS = MAX_CHUNK_BLOCKS * (uint)AuxiliaryChunk.size;
        private fixed byte auxiliaryChunks[(int)SIZEOF_AUXILIARY_CHUNKS];
        Chunk c;
    }*/
    unsafe struct AuxiliaryChunk {
        bool isStable;
        unsafe struct AuxiliaryBlock {
            bool isStable;
        }
    }
}
