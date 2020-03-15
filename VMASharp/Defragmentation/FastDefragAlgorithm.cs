﻿using System;
using System.Collections.Generic;
using Silk.NET.Vulkan;
using VMASharp;

namespace VMASharp.Defragmentation
{
    internal sealed class FastDefragAlgorithm : DefragmentationAlgorithm
    {
        private readonly bool overlappingMoveSupported;
        private int allocationCount;
        private bool allAllocations;

        private ulong bytesMoved;
        private int allocationsMoved;

        private readonly List<BlockInfo> blockInfos = new List<BlockInfo>();

        public FastDefragAlgorithm(VulkanMemoryAllocator allocator, BlockList list, uint currentFrame, bool overlappingMoveSupported) : base(allocator, list, currentFrame)
        {
            this.overlappingMoveSupported = overlappingMoveSupported;
        }

        public override ulong BytesMoved => throw new NotImplementedException();

        public override int AllocationsMoved => throw new NotImplementedException();

        public override void AddAll()
        {
            throw new NotImplementedException();
        }

        public override void AddAllocation(Allocation alloc, out bool changed)
        {
            throw new NotImplementedException();
        }

        public override Result Defragment(ulong maxBytesToMove, int maxAllocationsToMove, DefragmentationFlags flags, out DefragmentationMove[] moves)
        {
            throw new NotImplementedException();
        }

        private void PreprocessMetadata()
        {

        }

        private void PostprocessMetadata()
        {

        }

        private void InsertSuballoc(BlockMetadata_Generic metadata, Suballocation suballoc)
        {

        }

        private struct BlockInfo
        {
            public int OrigBlockIndex;
        }

        private class FreeSpaceDatabase
        {
            private const int MaxCount = 4;

            private FreeSpace[] FreeSpaces = new FreeSpace[MaxCount];

            public FreeSpaceDatabase()
            {
                for (int i = 0; i < FreeSpaces.Length; ++i)
                {
                    FreeSpaces[i].BlockInfoIndex = -1;
                }
            }

            public void Register(int blockInfoIndex, ulong offset, ulong size)
            {
                if (size < Helpers.MinFreeSuballocationSizeToRegister)
                {
                    return;
                }

                int bestIndex = -1;
                for (int i = 0; i < FreeSpaces.Length; ++i)
                {
                    ref FreeSpace space = ref FreeSpaces[i];

                    if (space.BlockInfoIndex == -1)
                    {
                        bestIndex = i;
                        break;
                    }

                    if (space.Size < size && (bestIndex == -1 || space.Size < FreeSpaces[bestIndex].Size))
                    {
                        bestIndex = i;
                    }
                }

                if (bestIndex != -1)
                {
                    ref FreeSpace bestSpace = ref FreeSpaces[bestIndex];

                    bestSpace.BlockInfoIndex = blockInfoIndex;
                    bestSpace.Offset = offset;
                    bestSpace.Size = size;
                }
            }

            public bool Fetch(ulong alignment, ulong size, out int blockInfoIndex, out ulong destOffset)
            {
                int bestIndex = -1;
                ulong bestFreeSpaceAfter = 0;

                for (int i = 0; i < FreeSpaces.Length; ++i)
                {
                    ref FreeSpace space = ref FreeSpaces[i];

                    if (space.BlockInfoIndex == -1)
                        break;

                    ulong tmpOffset = Helpers.AlignUp(space.Offset, alignment);

                    if (tmpOffset + size <= space.Offset + space.Size)
                    {
                        ulong freeSpaceAfter = (space.Offset + space.Size) - (tmpOffset + size);

                        if (bestIndex == -1 || freeSpaceAfter > bestFreeSpaceAfter)
                        {
                            bestIndex = i;
                            bestFreeSpaceAfter = freeSpaceAfter;
                        }
                    }
                }

                if (bestIndex != -1)
                {
                    ref FreeSpace bestSpace = ref FreeSpaces[bestIndex];

                    blockInfoIndex = bestSpace.BlockInfoIndex;
                    destOffset = Helpers.AlignUp(bestSpace.Offset, alignment);

                    if (bestFreeSpaceAfter >= Helpers.MinFreeSuballocationSizeToRegister)
                    {
                        ulong alignmentPlusSize = (destOffset - bestSpace.Offset) + size;

                        bestSpace.Offset += alignmentPlusSize;
                        bestSpace.Size -= alignmentPlusSize;
                    }
                    else
                    {
                        bestSpace.BlockInfoIndex = -1;
                    }

                    return true;
                }

                blockInfoIndex = default;
                destOffset = default;
                return false;
            }

            private struct FreeSpace
            {
                public int BlockInfoIndex;
                public ulong Offset, Size;
            }
        }
    }
}