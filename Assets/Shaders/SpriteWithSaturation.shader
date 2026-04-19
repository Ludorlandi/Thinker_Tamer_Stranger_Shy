Shader "Custom/SpriteWithSaturation"
{
    Properties
    {
        _MainTex    ("Sprite Texture", 2D) = "white" {}
        _Color      ("Tint", Color) = (1,1,1,1)
        _Saturation ("Saturation", Range(0,1)) = 1
    }
    SubShader
    {
        Tags
        {
            "Queue"          = "Transparent"
            "RenderType"     = "Transparent"
            "IgnoreProjector"= "True"
            "PreviewType"    = "Plane"
            "CanUseSpriteAtlas" = "True"
        }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4    _Color;
            half      _Saturation;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
            struct v2f    { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                o.color  = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                // Luminanza percettiva (BT.601)
                half lum   = dot(col.rgb, half3(0.299, 0.587, 0.114));
                col.rgb    = lerp(fixed3(lum, lum, lum), col.rgb, _Saturation);
                return col;
            }
            ENDCG
        }
    }
}
