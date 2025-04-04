using System;
using Unity.Mathematics;
using UnityEngine;

namespace Beakstorm.Simulation.Collisions.SDF
{
    /// <summary>
    /// Heavily pulled from https://github.com/SebLague/Ray-Tracing/blob/main/Assets/Scripts/BVH.cs
    /// </summary>
    public class BVH<TBounds, TSdfData> where TBounds : IBounds, ISdfData<TSdfData>
    {
        private int _nodeIndex;
        
        public BVH(TBounds[] items, int length, ref BVHItem[] allItems, ref Node[] nodeList, ref TSdfData[] result)
        {
            BoundingBox bounds = new BoundingBox();
            _nodeIndex = 0;
            
            for (int i = 0; i < length; i++)
            {
                float3 min = items[i].BoundsMin();
                float3 max = items[i].BoundsMax();
                float3 center = (max + min) / 2;
                allItems[i] = new BVHItem(center, min, max, i);
                bounds.GrowToInclude(min, max);
            }

            AddToNodeList(ref nodeList, new Node(bounds));
            Split(ref nodeList, allItems, 0, 0, allItems.Length);

            for (int i = 0; i < allItems.Length; i++)
            {
                BVHItem item = allItems[i];
                result[i] = items[item.Index].SdfData();
            }
        }

        private void Split(ref Node[] nodeList, BVHItem[] allItems, int parentIndex, int globalStart, int itemNum, int depth = 0)
        {
            const int MaxDepth = 8;
            Node parent = nodeList[parentIndex];
            float3 size = parent.CalculateBoundsSize();
            float parentCost = NodeCost(size, itemNum);

            ChooseSplit(allItems, parent, globalStart, itemNum, out int splitAxis, out float splitPos, out float cost);

            if (cost < parentCost && depth < MaxDepth)
            {
                BoundingBox boundsLeft = new();
                BoundingBox boundsRight = new();
                int numLeft = 0;

                for (int i = globalStart; i < globalStart + itemNum; i++)
                {
                    BVHItem item = allItems[i];
                    if (item.Center[splitAxis] < splitPos)
                    {
                        boundsLeft.GrowToInclude(item.Min, item.Max);
                        BVHItem swap = allItems[globalStart + numLeft];
                        allItems[globalStart + numLeft] = item;
                        allItems[i] = swap;
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

                int childIndexLeft = AddToNodeList(ref nodeList, new(boundsLeft, startLeft, 0));
                int childIndexRight = AddToNodeList(ref nodeList, new(boundsRight, startRight, 0));

                parent.StartIndex = childIndexLeft;
                nodeList[parentIndex] = parent;
                
                Split(ref nodeList, allItems, childIndexLeft, globalStart, numLeft, depth + 1);
                Split(ref nodeList, allItems, childIndexRight, globalStart + numLeft, numRight, depth + 1);
            }
            else
            {
                parent.StartIndex = globalStart;
                parent.ItemCount = itemNum;
                nodeList[parentIndex] = parent;
            }
        }

        private void ChooseSplit(BVHItem[] allItems, Node node, int start, int count, out int bestAxis, out float bestPos, out float bestCost)
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
                    float cost = EvaluateSplit(allItems, axis, splitPos, start, count);
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        bestPos = splitPos;
                        bestAxis = axis;
                    }
                }
            }
        }

        private int AddToNodeList(ref Node[] nodeList, Node node)
        {
            if (_nodeIndex >= nodeList.Length)
            {
                Array.Resize(ref nodeList, nodeList.Length * 2);
            }

            int nodeIndex = _nodeIndex;
            nodeList[_nodeIndex++] = node;
            return nodeIndex;
        }
        
        private float EvaluateSplit(BVHItem[] allItems, int splitAxis, float splitPos, int start, int count)
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
        
        private static float NodeCost(float3 size, int numTriangles)
        {
            float halfArea = size.x * size.y + size.x * size.z + size.y * size.z;
            return halfArea * numTriangles;
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
        public readonly float3 Center;
        public readonly float3 Min;
        public readonly float3 Max;
        public readonly int Index;

        public BVHItem(float3 center, float3 min, float3 max, int index)
        {
            Center = center;
            Min = min;
            Max = max;
            Index = index;
        }
    }
}
