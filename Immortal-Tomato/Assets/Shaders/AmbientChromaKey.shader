Shader "ImmortalTomato/AmbientChromaKey"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _KeyTolerance ("Magenta Tolerance", Range(0, 1)) = 0.30
        _KeySoftness ("Soft Edge", Range(0.001, 0.5)) = 0.08
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            sampler2D _MainTex;
            float _KeyTolerance;
            float _KeySoftness;
            v2f vert (appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; return o; }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv) * i.color;
                // Magenta requires both red and blue to dominate green. The
                // soft threshold removes edge fringe without affecting reds.
                float magentaDominance = min(color.r, color.b) - color.g;
                float key = smoothstep(_KeyTolerance, _KeyTolerance + _KeySoftness, magentaDominance);
                color.rgb = lerp(color.rgb, 0, key);
                color.a *= 1 - key;
                return color;
            }
            ENDCG
        }
    }
}
