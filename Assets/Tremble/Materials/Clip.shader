// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024 TinyGoose Ltd., All Rights Reserved.
//

Shader "Unlit/NoDraw"
{
    // This is an incredibly dumb shader, until such time as I can split render and collision
    // meshes up easily. It just discards all pixels thrown at it :)
    
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "ForceNoShadowCasting"="On" }
        LOD 100

        Pass
        {
            CGPROGRAM
            
            #pragma vertex EmptyVertex
            #pragma fragment EmptyFragment
            
            // Do literally nothing
            void EmptyVertex() { }

            // Discard all pixels, or failing that output a transparent one
            fixed4 EmptyFragment() : SV_Target
            {
                discard;
                return fixed4(0., 0., 0., 0.); // Required on Metal
            }
            ENDCG
        }
    }
}
