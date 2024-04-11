// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/E_SDFShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Alpha ("Alpha", Range(0, 10)) = 1.0
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"
        }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull front
        LOD 100

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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float _Alpha;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Box SDF function.
            float boxSDF(float2 p, float2 b, float2 s)
            {
                float2 d = abs(p - b) - s;
                return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
            }

            // Smootherstep function.
            float smootherstep(float edge0, float edge1, float x)
            {
                x = clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0);
                return x * x * x * (x * (x * 6 - 15) + 10);
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv * 2.0 - 1.0;

                // Define the 5x5 grid for the "E" character.
                float d = min(
                    min(
                        // Horizontal bars
                        boxSDF(uv, float2(-0.2, 0.4), float2(0.5, 0.1)),
                        min(
                            boxSDF(uv, float2(-0.2, 0.0), float2(0.5, 0.1)),
                            boxSDF(uv, float2(-0.2, -0.4), float2(0.5, 0.1))
                        )
                    ),
                    // Vertical bar
                    boxSDF(uv, float2(-0.6, 0.0), float2(0.1, 0.5))
                );

                // Apply the SDF function and the size property.
                float sdf = smootherstep(0.0, 0, d);
                return float4(sdf, sdf, sdf, _Alpha);
            }
            ENDCG
        }
    }
}