Shader "Custom/MoveRange/StencilWriter"
{
    SubShader
    {
        // SpriteRenderer(기본 Transparent 큐)까지 모두 그려진 뒤,
        // 암전 오버레이 직전에 스텐실 영역을 기록합니다.
        Tags { "Queue"="Transparent+99" "RenderType"="Opaque" }
        
        ColorMask 0 // 핵심: 화면에 어떠한 픽셀(색상)도 그리지 않음
        ZWrite Off  // 깊이 값도 쓰지 않음
        ZTest Always
        Cull Off

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
