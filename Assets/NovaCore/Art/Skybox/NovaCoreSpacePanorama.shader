// Nova Core Órbita - Skybox equirectangular (URP / Unity 6)
// Usa T_Skybox_DeepSpace_2K.png. La imagen gira con la órbita (la controla OrbitalEnvironment.cs)
// para que estrellas, Vía Láctea y Sol pintado se muevan juntos con el Sol real de la escena.
Shader "NovaCore/SpacePanorama"
{
    Properties
    {
        _MainTex ("Panorama equirectangular", 2D) = "black" {}
        [HDR] _Tint ("Tinte", Color) = (1, 1, 1, 1)
        _Exposure ("Exposición", Range(0.0, 8.0)) = 1.3
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }
        Cull Off
        ZWrite Off

        Pass
        {
            Name "SpacePanorama"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Tint;
                half _Exposure;
            CBUFFER_END

            // Lo pone OrbitalEnvironment.cs cada frame: mundo -> marco de la textura
            float4x4 _NovaSkyRotation;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 d = mul((float3x3)_NovaSkyRotation, normalize(i.dir));
                if (dot(d, d) < 0.5) d = normalize(i.dir); // global sin asignar: no rotar
                d = normalize(d);

                // Misma convención que Skybox/Panoramic de Unity
                float lon = atan2(d.z, d.x);
                float lat = acos(clamp(d.y, -1.0, 1.0));
                float2 uv = float2(0.5 - lon * 0.15915494, 1.0 - lat * 0.31830989);

                // Evita la línea en la costura (u salta de 1 a 0): usar derivadas de la variante continua
                float uAlt = frac(uv.x + 0.5) - 0.5;
                bool useMain = fwidth(uv.x) <= fwidth(uAlt);
                float2 gx = useMain ? ddx(uv) : ddx(float2(uAlt, uv.y));
                float2 gy = useMain ? ddy(uv) : ddy(float2(uAlt, uv.y));

                half3 col = SAMPLE_TEXTURE2D_GRAD(_MainTex, sampler_MainTex, uv, gx, gy).rgb;
                return half4(col * _Tint.rgb * _Exposure, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
