Shader "DynaMak/SceneVoxelizer256"
{
    Properties
    {
    }
    SubShader
    {
        Cull Off
        ZTest Always
        
        Pass
        {
            CGPROGRAM
            
            #pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
        
            uint3 _VoxelizerResolution;
            RWTexture2D<uint> _SliceMap;
            

            /// <summary>
            /// Returns bitmask that fills all bits up until and including the given depth.
            /// </summary>
            /// <param name="depth">Voxel depth of the current fragment</param>
            /// 
            inline uint4 DepthToBitmaskOverflow(int4 depth)
            {
                return depth >= 32 ?~0 : depth < 0 ? 0 : (1u << depth) - 1u;
            }

            
            /// <summary>
            /// Fills a depth-slice of a SliceMap at the given coordinates with the XOR-operator.
            /// For a watertight mesh, this turns the SliceMap into a bool-volume of occupied voxels.
            /// </summary>
            /// <param name="slicemap">SliceMap of depth 256 (4*2 pixel per depth)</param>
            /// <param name="volumeId">Voxel coordinate of the current fragment</param>
            ///
            void DepthBitmapXor256(RWTexture2D<uint> slicemap, uint3 volumeId)
            {
                uint2 texId = uint2(volumeId.x * 4, volumeId.z * 2);
                
                int4 depth0 = int4(
                    volumeId.y -  0,
                    volumeId.y - 32,
                    volumeId.y - 64,
                    volumeId.y - 96
                );
                int4 depth1 = int4(
                    volumeId.y - 128,
                    volumeId.y - 160,
                    volumeId.y - 192,
                    volumeId.y - 224
                );
                
                uint4 xorBitmasks0 = DepthToBitmaskOverflow(depth0);
                uint4 xorBitmasks1 = DepthToBitmaskOverflow(depth1);

                InterlockedXor(slicemap[texId + uint2(0,0)], xorBitmasks0.r);
                InterlockedXor(slicemap[texId + uint2(1,0)], xorBitmasks0.g);
                InterlockedXor(slicemap[texId + uint2(2,0)], xorBitmasks0.b);
                InterlockedXor(slicemap[texId + uint2(3,0)], xorBitmasks0.a);

                InterlockedXor(slicemap[texId + uint2(0,1)], xorBitmasks1.r);
                InterlockedXor(slicemap[texId + uint2(1,1)], xorBitmasks1.g);
                InterlockedXor(slicemap[texId + uint2(2,1)], xorBitmasks1.b);
                InterlockedXor(slicemap[texId + uint2(3,1)], xorBitmasks1.a);
            }
            
            
            struct appdata
            {
                float4 vertex : POSITION;
            };
        
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOOORD0;
            };
        
        
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }
        
            float frag (v2f i) : SV_Target
            {
                float depthRemap = (i.vertex.z);
                uint depth = depthRemap * _VoxelizerResolution.y;
        
                float2 sampleUv = i.screenPos.xy / i.screenPos.w;
                uint3 idVolume = uint3(sampleUv.x * _VoxelizerResolution.x, depth, sampleUv.y * _VoxelizerResolution.z);

                DepthBitmapXor256(_SliceMap, idVolume);

                return 0;
            }
            
            ENDCG
        }
    }
    FallBack Off
}
