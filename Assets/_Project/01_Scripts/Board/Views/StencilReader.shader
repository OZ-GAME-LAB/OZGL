Shader "Custom/MoveRange/StencilReader"
{
    Properties
    {
        _Color ("Overlay Color", Color) = (0, 0, 0, 0.7) // 반투명 검은색
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 6
    }
    SubShader
    {
        // 기본 Transparent 큐의 SpriteRenderer보다 나중에 그려야
        // 타일이 암전 오버레이 위에 다시 그려지지 않습니다.
        Tags { "Queue"="Transparent+100" "RenderType"="Transparent" }
        
        Blend SrcAlpha OneMinusSrcAlpha // 반투명 블렌딩 허용
        ZWrite Off
        ZTest Always
        Cull Off

        Stencil
        {
            Ref 1
            Comp [_StencilComp]
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };
            
            float4 _Color;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
