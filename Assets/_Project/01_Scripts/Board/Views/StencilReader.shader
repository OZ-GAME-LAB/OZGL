Shader "Custom/MoveRange/StencilReader"
{
    Properties
    {
        _Color ("Overlay Color", Color) = (0, 0, 0, 0.7) // 반투명 검은색
    }
    SubShader
    {
        // StencilWriter가 도장을 다 찍은 후(Geometry)에 그려지도록 설정
        Tags { "Queue"="Geometry" "RenderType"="Transparent" }
        
        Blend SrcAlpha OneMinusSrcAlpha // 반투명 블렌딩 허용
        ZWrite Off

        Stencil
        {
            Ref 1
            Comp NotEqual // 핵심: 스텐실 버퍼 값이 '1'이 아닌 곳에만 그려라!
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