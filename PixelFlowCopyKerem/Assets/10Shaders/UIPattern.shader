Shader "Unlit/UIPattern"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ScrollXSpeed ("X Kayma Hizi", Float) = -0.05
        _ScrollYSpeed ("Y Kayma Hizi", Float) = -0.05
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
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
                float2 texcoord  : TEXCOORD0;
            };
            
            fixed4 _Color;
            sampler2D _MainTex;
            // --- Tiled Image için gerekli sihirli deðiþken ---
            float4 _MainTex_ST; 
            float _ScrollXSpeed;
            float _ScrollYSpeed;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                
                // --- Tiled Image Uyumluluðu Ýçin Hesaplama ---
                // Unity'nin TRANSFORM_TEX makrosu, Tiled Image'ýn PPUM ve boyutuyla 
                // gelen UV esnemelerini (tiling/offset) bu hesaba katar.
                OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex);
                
                // --- Sonra da zamanla kaydýrýyoruz ---
                float2 offset = float2(_Time.y * _ScrollXSpeed, _Time.y * _ScrollYSpeed);
                OUT.texcoord += offset;
                
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
{
    // frac() fonksiyonu tile (tekrarlama) yapar
    float2 tiledUV = frac(IN.texcoord);
    
    // Shader'ýn her karede GPU tarafýndan hesaplandýðýndan emin olalým
    fixed4 c = tex2D(_MainTex, tiledUV) * IN.color;
    
    // Alpha 0 ise (boþluklar) çizme
    clip(c.a - 0.01); 

    c.rgb *= c.a;
    return c;
}
        ENDCG
        }
    }
}