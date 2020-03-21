﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RtspClientSharp.Utils;

namespace RtspClientSharp.UnitTests.Utils
{
    [TestClass]
    public class ChunksArrayTests
    {
        [TestMethod]
        public void Insert_SeveralChunks_CanBeReadProperly()
        {
            const int testChunksCount = 100;
            var chunkList = new List<ArraySegment<byte>>();

            for (int i = 0; i < testChunksCount; i++)
            {
                var chunkBytes = new byte[] {1, 2, 3, 4, 5, (byte) i};
                var chunkSegment = new ArraySegment<byte>(chunkBytes);
                chunkList.Add(chunkSegment);
            }
            
            var chunkArray = new ChunksArray(1500, testChunksCount);

            for (int i = 0; i < testChunksCount; i++)
                chunkArray.Insert(chunkList[i]);
            
            for (int i = 0; i < testChunksCount; i++)
                Assert.IsTrue(chunkArray[i].SequenceEqual(chunkArray[i]));
        }

        [TestMethod]
        public void Insert_NewChunk_CountShouldBeSetToOne()
        {
            var chunkBytes = new byte[] { 1, 2, 3, 4, 5 };
            var chunkSegment = new ArraySegment<byte>(chunkBytes);
            var chunkArray = new ChunksArray(1500, 1);

            chunkArray.Insert(chunkSegment);

            Assert.AreEqual(1, chunkArray.Count);
        }

        [TestMethod]
        public void Clear_SeveralTestChunks_EmptyArray()
        {
            int count = 10;
            var chunkBytes = new byte[] { 1, 2, 3, 4, 5 };
            var chunkSegment = new ArraySegment<byte>(chunkBytes);
            var chunkArray = new ChunksArray(1500, 10);

            for(int i = 0; i < count; i++)
                chunkArray.Insert(chunkSegment);
            chunkArray.Clear();

            Assert.AreEqual(0, chunkArray.Count);
        }

        [TestMethod]
        public void RemoveAt_RemoveMidpoint_ValidArray()
        {
            int testChunksCount = 100;
            var chunkList = new List<byte[]>();
            var indexMap = new List<int>();
            var chunkArray = new ChunksArray(1500, testChunksCount);
            for (int i = 0; i < testChunksCount; i++)
            {
                var chunkBytes = new byte[] { 1, 2, 3, 4, 5, (byte)i };
                var chunkSegment = new ArraySegment<byte>(chunkBytes);
                int index = chunkArray.Insert(chunkSegment);
                chunkList.Add(chunkBytes);
                indexMap.Add(index);
            }

            int removeIndex = testChunksCount / 2;
            chunkArray.RemoveAt(removeIndex);
            chunkList.RemoveAt(removeIndex);
            indexMap.RemoveAt(removeIndex);

            Assert.AreEqual(--testChunksCount, chunkArray.Count);
            for (int i = 0; i < testChunksCount; i++)
                Assert.IsTrue(chunkList[i].SequenceEqual(chunkArray[indexMap[i]]));
        }
    }
}
