Shader "Custom/MoveRange/StencilWriter"
{
    SubShader
    {
        // 일반 렌더링(Geometry)보다 아주 살짝 먼저 그려지도록 큐 설정
        Tags { "Queue"="Geometry-1" "RenderType"="Opaque" }
        
        ColorMask 0 // 핵심: 화면에 어떠한 픽셀(색상)도 그리지 않음
        ZWrite Off  // 깊이 값도 쓰지 않음

        Stencil
        {
            Ref 1           // 1이라는 값을
            Comp Always     // 항상 (무조건)
            Pass Replace    // 스텐실 버퍼에 덮어씌운다
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            half4 frag(v2f i) : SV_Target { return half4(0,0,0,0); }
            ENDCG
        }
    }
}