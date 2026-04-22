Shader "Sprites/GlitchTile"
{
    Properties
    {
        _MainTex     ("Texture",     2D)           = "white" {}
        _Intensity   ("Intensity",   Range(0, 1))  = 1
        _GlitchTime  ("Glitch Time", Float)        = 0
        _GlitchColor ("Glitch Color", Color)       = (0.35, 0.65, 0.50, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "RenderType"        = "Transparent"
            "IgnoreProjector"   = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float     _Intensity;
            float     _GlitchTime;
            float4    _GlitchColor;

            // ---- hash helpers ----
            float hash2(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }
            float hash1(float x)
            {
                return frac(sin(x * 127.1) * 43758.5453);
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.vertex   = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color    = v.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv  = IN.texcoord;
                float  t   = _GlitchTime;
                float  ins = _Intensity;

                // ---- Coarse horizontal slice displacement ----
                float sliceId      = floor(uv.y * 14.0);
                float sliceTrigger = step(1.0 - ins * 0.9, hash2(float2(sliceId, 0.0)));
                float sliceOffset  = (hash2(float2(sliceId, floor(t * 8.0))) - 0.5) * 0.20 * sliceTrigger * ins;

                // ---- Fine scanline displacement ----
                float thinId      = floor(uv.y * 70.0);
                float thinTrigger = step(1.0 - ins * 0.55, hash2(float2(thinId, floor(t * 22.0))));
                float thinOffset  = (hash2(float2(thinId, t)) - 0.5) * 0.05 * thinTrigger * ins;

                float totalX = sliceOffset + thinOffset;
                float2 uvG = uv + float2(totalX, 0.0);

                // ---- RGB channel aberration ----
                float split = 0.028 * ins;
                float2 uvR  = uvG + float2( split, 0.0);
                float2 uvB  = uvG - float2( split, 0.0);

                // ---- Digital pixel noise per channel ----
                float ns  = 80.0;
                float nt  = 0.75;
                float noiseR = hash2(float2(floor(uvR.x * ns), floor(uvR.y * ns * nt) + floor(t * 26.0)));
                float noiseG = hash2(float2(floor(uvG.x * ns), floor(uvG.y * ns * nt) + floor(t * 31.0) + 1.0));
                float noiseB = hash2(float2(floor(uvB.x * ns), floor(uvB.y * ns * nt) + floor(t * 26.0) + 2.0));

                // ---- Scanline darkening ----
                float scanDark = step(0.5, frac(uv.y * 55.0)) * 0.18 * ins;

                // ---- Bright horizontal flash lines ----
                float flashLine = step(0.91, hash2(float2(floor(uv.y * 32.0), floor(t * 16.0))));
                float3 flashCol = _GlitchColor.rgb * float3(flashLine * 0.55, flashLine * 0.55, flashLine * 0.35) * ins;

                // ---- Compose final color ----
                float3 col;
                col.r = (noiseR + 0.08) * _GlitchColor.r * ins;
                col.g = (noiseG + 0.08) * _GlitchColor.g * ins;
                col.b = (noiseB + 0.08) * _GlitchColor.b * ins;
                col  += flashCol;
                col  *= (1.0 - scanDark);

                // ---- Alpha ----
                float alpha = ins * 0.96;

                // Full-frame glitch flicker
                float flicker = step(0.965, hash1(floor(t * 45.0)));
                alpha = saturate(alpha + flicker * 0.55);
                col   = lerp(col, _GlitchColor.rgb * 1.3, flicker * ins * 0.55);

                return fixed4(col, alpha * IN.color.a);
            }
            ENDCG
        }
    }
}
