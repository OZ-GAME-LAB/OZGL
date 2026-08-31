Shader "OZGL/Background/PaperBackground"
{
    Properties
    {
        [Header(Base)]
        _BaseColor ("Base Color", Color) = (0.9, 0.88, 0.82, 1)

        [Header(Paper Noise)]
        _PaperScale ("Paper Scale", Float) = 5
        _PaperStrength ("Paper Strength", Range(0, 0.3)) = 0.06

        [Header(Grain)]
        _GrainScale ("Grain Scale", Float) = 180
        _GrainStrength ("Grain Strength", Range(0, 0.3)) = 0.035

        [Header(Fiber)]
        _FiberScale ("Fiber Scale", Float) = 100
        _FiberStrength ("Fiber Strength", Range(0, 0.2)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _BaseColor;

            float _PaperScale;
            float _PaperStrength;

            float _GrainScale;
            float _GrainStrength;

            float _FiberScale;
            float _FiberStrength;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);

                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(
                    lerp(a, b, u.x),
                    lerp(c, d, u.x),
                    u.y
                );
            }

            float FBM(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;

                for (int i = 0; i < 4; i++)
                {
                    value += Noise(p) * amplitude;

                    p *= 2.03;
                    amplitude *= 0.5;
                }

                return value;
            }

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float paper =
                    FBM(uv * _PaperScale);

                paper =
                    (paper - 0.5)
                    * _PaperStrength;

                float grain =
                    Noise(uv * _GrainScale);

                grain =
                    (grain - 0.5)
                    * _GrainStrength;

                float fiberNoise =
                    Noise(uv * 15.0);


                float horizontalFiber =
                    sin(
                        (
                            uv.y
                            + fiberNoise * 0.025
                        )
                        * _FiberScale
                    );

                horizontalFiber =
                    pow(
                        saturate(
                            1.0 - abs(horizontalFiber)
                        ),
                        8.0
                    );


                float verticalFiber =
                    sin(
                        (
                            uv.x
                            + fiberNoise * 0.02
                        )
                        * (_FiberScale * 0.55)
                    );

                verticalFiber =
                    pow(
                        saturate(
                            1.0 - abs(verticalFiber)
                        ),
                        10.0
                    );


                float fiber =
                    (
                        horizontalFiber
                        + verticalFiber * 0.35
                    )
                    * _FiberStrength;

                float variation =
                    paper
                    + grain
                    + fiber;


                float3 color =
                    _BaseColor.rgb
                    + variation;


                return fixed4(color, _BaseColor.a);
            }

            ENDCG
        }
    }

    FallBack "Unlit/Color"
}