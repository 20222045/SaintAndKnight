// UI picture shader that bends up to four elliptical regions of the texture by a per-region offset
// (SKBustMorph drives the offsets with springs). Point-sampled art stays on its pixel grid.
Shader "SaintKnight/BustMorph"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _Region[4];   // xy centre (uv), zw radius (uv); zw = 0 means unused
            float4 _Offset[4];   // xy offset (uv), zw squash/stretch (relative)

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 2026-11-19 user: "the whole breast should move". Inside the region ellipse (the breast outline)
                // the picture moves as one piece; only a band out to 1.6x the radius stretches to join the body.
                // Overlapping bands are averaged, not summed, so the cleavage never moves twice as far.
                float2 shift = 0;
                float total = 0;
                for (int k = 0; k < 4; k++)
                {
                    float2 r = max(_Region[k].zw, 1e-5);
                    float2 rel = i.uv - _Region[k].xy;
                    float e = length(rel / r);
                    float w = (1.0 - smoothstep(1.0, 1.6, e)) * step(1e-5, _Region[k].z);
                    shift += (_Offset[k].xy + rel * _Offset[k].zw) * w;
                    total += w;
                }
                float2 uv = i.uv - shift / max(total, 1.0);
                return tex2D(_MainTex, uv) * i.color;
            }
            ENDCG
        }
    }
}
