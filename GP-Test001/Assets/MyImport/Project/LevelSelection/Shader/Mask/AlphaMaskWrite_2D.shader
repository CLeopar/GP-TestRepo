Shader "Custom/AlphaMaskWrite_2D"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GroupID ("Group ID", Int) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent-1" "RenderType"="Transparent" }
        
        Pass
        {
            Stencil
            {
                Ref [_GroupID]
                Comp Always
                Pass Replace
                WriteMask 255
                ReadMask 255
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                clip(col.a - 0.1); // 透明度小于0.1的不写入模板
                return col;
            }
            ENDCG
        }
    }
}