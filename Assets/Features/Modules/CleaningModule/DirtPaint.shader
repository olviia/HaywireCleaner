Shader "Hidden/Cleanbot/DirtPaint"
{
    SubShader
    {
        Pass
        {
            Name "PaintSweptSegment"
            
            Cull    Off
            ZWrite  Off
            ZTest   Always
            Blend   One One
            BlendOp Max
            ColorMask R
            
            HLSLPROGRAM
            #pragma vertex  vert
            #pragma fragment frag
            #pragma target 3.0

            float4 _PaintRectUV; //xy = min uv, zw = max uv
            float4 _PaintRectWorld; //xy = min world XZ, zw = max world XZ
            float4 _BrushSegment; //xy = A world XZ, zw = B world XZ
            float _BrushRadius; //metres
            float _BrushSoftness; //0 = hard, 1 = soft
            float _FlipY; //+1 or -1

            struct VAryings
            {
                float4 positionCS   : SV_POSITION;
                float2 worldXZ      : TEXCOORD0;
            };
        }
    }
}
