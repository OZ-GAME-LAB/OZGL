Shader "Custom/Board/TimeOfDayOverlay"
{
    Properties
    {
        _Tint ("Time Of Day Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        // 일반 타일 다음, 이동 범위 스텐실(Transparent+99 이상)보다 먼저 그립니다.
        Tags { "Queue"="Transparent+50" "RenderType"="Transparent" }

        Blend DstColor Zero
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
            };

            fixed4 _Tint;

            v2f vert(appdata input)
            {
                v2f output;
                output.position = UnityObjectToClipPos(input.vertex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                return fixed4(_Tint.rgb, 1.0);
            }
            ENDCG
        }
    }
}
