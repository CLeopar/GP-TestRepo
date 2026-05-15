Shader "Custom/SpriteOutline"
{
    Properties
    {
        _MainTex      ("Texture",       2D)    = "white" {}
        _Color        ("Tint",          Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,0,1)
        _OutlineWidth ("Outline Width", Float) = 2.0
        _OutlineEnabled ("Outline Enabled", Float) = 0.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            sampler2D _MainTex;
            float4    _MainTex_TexelSize;
            float4    _Color;
            float4    _OutlineColor;
            float     _OutlineWidth;
            float     _OutlineEnabled;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                o.color  = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color * _Color;

                if (_OutlineEnabled > 0.5)
                {
                    // 采样上下左右及斜角共8个方向，判断是否有不透明邻居
                    float2 texel = _MainTex_TexelSize.xy * _OutlineWidth;

                    float a = 0;
                    a += tex2D(_MainTex, i.uv + float2( texel.x,  0      )).a;
                    a += tex2D(_MainTex, i.uv + float2(-texel.x,  0      )).a;
                    a += tex2D(_MainTex, i.uv + float2( 0,        texel.y)).a;
                    a += tex2D(_MainTex, i.uv + float2( 0,       -texel.y)).a;
                    a += tex2D(_MainTex, i.uv + float2( texel.x,  texel.y)).a;
                    a += tex2D(_MainTex, i.uv + float2(-texel.x,  texel.y)).a;
                    a += tex2D(_MainTex, i.uv + float2( texel.x, -texel.y)).a;
                    a += tex2D(_MainTex, i.uv + float2(-texel.x, -texel.y)).a;

                    // 当前像素透明但周围有不透明像素 → 描边区域
                    if (col.a < 0.1 && a > 0.1)
                        return _OutlineColor;
                }

                return col;
            }
            ENDCG
        }
    }
}