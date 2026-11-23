Shader "SaintKnight/Preview/Corridor Stone"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 stonePixels : TEXCOORD0;
                half3 normal : TEXCOORD1;
                half3 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.stonePixels = input.positionOS.xy * 48.0;
                output.normal = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color.rgb;
                return output;
            }

            float Grain(float2 cell)
            {
                float3 p = frac(float3(cell.xyx) * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half lit = dot(normalize(input.normal), half3(-0.45h, 0.50h, -0.74h));
                half shade = 0.52h + 0.28h * step(0.38h, lit)
                                  + 0.20h * step(0.58h, lit)
                                  + 0.48h * step(0.82h, lit);
                float2 pixel = floor(input.stonePixels);
                half patch = step(0.50, Grain(floor(pixel / 5.0)));
                half pit = step(0.92, Grain(pixel + float2(62, 17)));
                half chip = step(0.93, Grain(pixel));
                return half4(input.color * shade * (0.85h + 0.22h * patch - 0.19h * pit + 0.37h * chip), 1);
            }
            ENDHLSL
        }
    }
}
