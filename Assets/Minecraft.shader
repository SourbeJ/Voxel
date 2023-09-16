Shader "Custom/TransparentWithLighting"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _BrightnessThreshold ("Brightness Threshold", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "Queue"="Transparent" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Cutoff;
            float _BrightnessThreshold;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                // Transférer la normale de l'espace de l'objet à l'espace du monde
                o.worldNormal = normalize(mul(float4(v.normal, 0), unity_WorldToObject).xyz);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                col.a = step(_Cutoff, max(max(col.r, col.g), col.b)); // Transparence basée sur le seuil

                // Utilisation de la luminosité pour ajuster la transparence
                float brightness = dot(i.worldNormal, float3(1, 1, 1));
                col.a *= step(_BrightnessThreshold, brightness);

                return col;
            }
            ENDCG
        }
    }
}