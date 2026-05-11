Shader "Custom/AlphaMaskRead_2D"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GroupID ("Group ID", Int) = 0
        [Toggle] _EditModePreview("Edit Mode Preview", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        
        Pass
        {
            Stencil
            {
                Ref [_GroupID]
                Comp Equal
                Pass Keep
            }
            
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;  // 接收Sprite Renderer的颜色
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;  // 传递颜色到片段着色器
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            int _GroupID;
            float _EditModePreview;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;  // 直接传递Sprite Renderer的颜色
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // 使用Sprite Renderer的颜色来控制透明度和颜色调制
                col *= i.color;
                
                // 编辑模式预览（不影响实际效果）
                if (_EditModePreview > 0.5)
                {
                    // 可选：添加预览效果
                }
                
                return col;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}