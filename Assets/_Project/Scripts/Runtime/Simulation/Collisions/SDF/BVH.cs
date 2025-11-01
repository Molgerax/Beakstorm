using System;
using Unity.Mathematics;
using UnityEngine;

namespace Beakstorm.Simulation.Collisions.SDF
{
    /// <summary>
    /// Heavily pulled from https://github.com/SebLague/Ray-Tracing/blob/main/Assets/Scripts/BVH.cs
    /// </summary>
    public static class BVH<TBounds, TSdfData> where TBounds : IBounds, ISdfData<TSdfData>, IValid
    {
        private const int MaxDepth = 8;
        private static int _nodeIndex;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="items">Array of items.</param>
        /// <param name="itemCount">Count of items within the array, as not read from unwritten array elements.</param>
        /// <param name="bvhItemArray">Array used for the BVH construction process.</param>
        /// <param name="nodeArray">Node array used to iteratively traverse the BVH.</param>
        /// <param name="sdfDataArray">Array of data that is ordered so that data can be accessed by traversing the NodeArray.</param>
        /// <returns></returns>
        public static int ConstructBVH(TBounds[] items, int itemCount, ref BVHItem[] bvhItemArray, ref Node[] nodeArray, ref TSdfData[] sdfDataArray)
        {
            if (items == null || items.Length == 0)
                return 0;
            
            BoundingBox bounds = new BoundingBox();
            _nodeIndex = 0;

            int invalidCount = 0;
            for (int i = 0; i < itemCount; i++)
            {
                if (!items[i].IsValid)
                {
                    invalidCount++;
                }
                
                float3 min = items[i].BoundsMin();
                float3 max = items[i].BoundsMax();
                bvhItemArray[i] = new BVHItem(min, max, i);
                bounds.GrowToInclude(min, max);
            }

            AddToNodeList(ref nodeArray, new Node(bounds));
            Split(ref nodeArray, bvhItemArray, 0, 0, itemCount - invalidCount);

            for (int i = 0; i < bvhItemArray.Length; i++)
            {
                if (i >= itemCount - invalidCount)
                    continue;
                
                BVHItem item = bvhItemArray[i];
                sdfDataArray[i] = items[item.Index].SdfData();
            }

            return _nodeIndex;
        }

        
        private static void Split(ref Node[] nodeArray, BVHItem[] bvhItemArray, int parentIndex, int globalStart, int itemNum, int depth = 0)
        {
            Node parent = nodeArray[parentIndex];
            float3 size = parent.CalculateBoundsSize();
            float parentCost = NodeCost(size, itemNum);

            ChooseSplit(bvhItemArray, parent, globalStart, itemNum, out int splitAxis, out float splitPos, out float cost);

            if (cost < parentCost && depth < MaxDepth)
            {
                BoundingBox boundsLeft = new();
                BoundingBox boundsRight = new();
                int numLeft = 0;

                for (int i = globalStart; i < globalStart + itemNum; i++)
                {
                    BVHItem item = bvhItemArray[i];
                    if (item.Center[splitAxis] < splitPos)
                    {
                        boundsLeft.GrowToInclude(item.Min, item.Max);
                        BVHItem swap = bvhItemArray[globalStart + numLeft];
                        bvhItemArray[globalStart + numLeft] = item;
                        bvhItemArray[i] = swap;
                        numLeft++;
                    }
                    else
                    {
                        boundsRight.GrowToInclude(item.Min, item.Max);
                    }
                }

                int numRight = itemNum - numLeft;
                int startLeft = globalStart + 0;
                int startRight = globalStart + numLeft;

                int childIndexLeft = AddToNodeList(ref nodeArray, new(boundsLeft, startLeft, 0));
                int childIndexRight = AddToNodeList(ref nodeArray, new(boundsRight, startRight, 0));

                parent.StartIndex = childIndexLeft;
                nodeArray[parentIndex] = parent;
                
                Split(ref nodeArray, bvhItemArray, childIndexLeft, globalStart, numLeft, depth + 1);
                Split(ref nodeArray, bvhItemArray, childIndexRight, globalStart + numLeft, numRight, depth + 1);
            }
            else
            {
                parent.StartIndex = globalStart;
                parent.ItemCount = itemNum;
                nodeArray[parentIndex] = parent;
            }
        }

        /// <summary>
        /// Chooses the most optimal plane along which to split the current node
        /// </summary>
        /// <param name="bvhItemArray"></param>
        /// <param name="node">Parent node to split</param>
        /// <param name="start">Index of the first BVHItem</param>
        /// <param name="count">Number of items in the parent node</param>
        /// <param name="bestAxis"></param>
        /// <param name="bestPos"></param>
        /// <param name="bestCost"></param>
        private static void ChooseSplit(BVHItem[] bvhItemArray, Node node, int start, int count, out int bestAxis, out float bestPos, out float bestCost)
        { 
            const int numSplitTests = 5;

            bestAxis = 0; bestPos = 0; bestCost = float.PositiveInfinity;
            if (count <= 1) 
                return;

            bestCost = float.MaxValue;

            for (int axis = 0; axis < 3; axis++)
            {
                for (int i = 0; i < numSplitTests; i++)
                {
                    float splitT = (i + 1) / (numSplitTests + 1f);
                    float splitPos = Mathf.Lerp(node.BoundsMin[bestAxis], node.BoundsMax[bestAxis], splitT);
                    float cost = EvaluateSplit(bvhItemArray, axis, splitPos, start, count);
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        bestPos = splitPos;
                        bestAxis = axis;
                    }
                }
            }
        }

        private static int AddToNodeList(ref Node[] nodeList, Node node)
        {
            if (_nodeIndex >= nodeList.Length)
            {
                Array.Resize(ref nodeList, nodeList.Length * 2);
            }

            int nodeIndex = _nodeIndex;
            nodeList[_nodeIndex++] = node;
            return nodeIndex;
        }
        
        private static float EvaluateSplit(BVHItem[] allItems, int splitAxis, float splitPos, int start, int count)
        {
            BoundingBox boundsLeft = new();
            BoundingBox boundsRight = new();
            int numLeft = 0;
            int numRight = 0;

            for (int i = start; i < start + count; i++)
            {
                BVHItem item = allItems[i];
                if (item.Center[splitAxis] < splitPos)
                {
                    boundsLeft.GrowToInclude(item.Min, item.Max);
                    numLeft++;
                }
                else
                {
                    boundsRight.GrowToInclude(item.Min, item.Max);
                    numRight++;
                }
            }

            float costA = NodeCost(boundsLeft.Size, numLeft);
            float costB = NodeCost(boundsRight.Size, numRight);
            return costA + costB;
        }
        
        
        private static float NodeCost(float3 size, int numItems)
        {
            float halfArea = size.x * size.y + size.x * size.z + size.y * size.z;
            float volume = size.x * size.y * size.z;
            return volume * numItems;
        }
    }

    public struct Node
    {
        public float3 BoundsMin;
        public float3 BoundsMax;

        public int StartIndex;
        public int ItemCount;
        
        public Node(BoundingBox bounds)
        {
            BoundsMin = bounds.Min;
            BoundsMax = bounds.Max;
            StartIndex = -1;
            ItemCount = -1;
        }
        
        public Node(BoundingBox bounds, int startIndex, int itemCount)
        {
            BoundsMin = bounds.Min;
            BoundsMax = bounds.Max;
            StartIndex = startIndex;
            ItemCount = itemCount;
        }
        
        public Node(IBounds bounds)
        {
            BoundsMin = bounds.BoundsMin();
            BoundsMax = bounds.BoundsMax();
            StartIndex = -1;
            ItemCount = -1;
        }
        
        public Node(IBounds bounds, int startIndex, int itemCount)
        {
            BoundsMin = bounds.BoundsMin();
            BoundsMax = bounds.BoundsMax();
            StartIndex = startIndex;
            ItemCount = itemCount;
        }
        
        public float3 CalculateBoundsSize() => BoundsMax - BoundsMin;
        public float3 CalculateBoundsCenter() => (BoundsMin + BoundsMax) / 2;
    }
    
    
    public readonly struct BVHItem
    {
        public readonly float3 Min;
        public readonly float3 Max;
        public readonly int Index;

        public float3 Center => (Min + Max) / 2f;

        public BVHItem(float3 min, float3 max, int index)
        {
            Min = min;
            Max = max;
            Index = index;
        }

        public bool IntersectsWith(BVHItem other)
        {
            float3 p = new float3(1,1,1) - math.step(other.Max, Min);
            float3 q = math.step(other.Min, Max);

            return math.all(p) && math.all(q);
        }
    }
}
