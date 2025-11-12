Shader "Brush/StandardDoubleSided_URP_SoftLit_MultiLight"
{
    Properties
    {
        _Color     ("Main Color", Color) = (1,1,1,1)
        _SpecColor ("Specular Color", Color) = (0.5,0.5,0.5,1)
        _Shininess ("Shininess", Range(0.01,1)) = 0.078125
        _MinLight  ("Minimum Light", Range(0,1)) = 0.25
        _MainTex   ("Base (RGB) Alpha (A)", 2D) = "white" {}
        _BumpMap   ("Normal Map", 2D) = "bump" {}
        _Cutoff    ("Alpha Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Off  LOD 400

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #define DECLARE_FOG_COORDS(name)
            #define TRANSFER_FOG_COORDS(o,pos)
            #define APPLY_FOG(coord,col)

            CBUFFER_START(UnityPerMaterial)
                half4 _Color, _SpecColor;
                half  _Shininess, _MinLight, _Cutoff;
                half4 _MainTex_ST;
            CBUFFER_END

            TEXTURE2D(_MainTex);  SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);  SAMPLER(sampler_BumpMap);

            struct Attributes
            {
                float4 vertex  : POSITION;
                float2 uv      : TEXCOORD0;
                float3 normal  : NORMAL;
                float4 tangent : TANGENT;
                float4 color   : COLOR;
            };

            struct Varyings
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 t0       : TEXCOORD2;
                float3 t1       : TEXCOORD3;
                float3 t2       : TEXCOORD4;
                half4  color    : COLOR;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.pos      = TransformObjectToHClip(v.vertex);
                o.uv       = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = TransformObjectToWorld(v.vertex);
                o.color    = v.color * _Color;

                float3 n = normalize(TransformObjectToWorldNormal(v.normal));
                float3 t = normalize(TransformObjectToWorldDir(v.tangent.xyz));
                float3 b = cross(n, t) * v.tangent.w;

                o.t0 = t;  o.t1 = b;  o.t2 = n;
                return o;
            }

            half4 frag (Varyings i, half vface : VFACE) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                if (tex.a < _Cutoff) discard;

                half3 albedo = tex.rgb * i.color.rgb;

                // Normal
                half3 tn = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, i.uv));
                tn.z *= vface;
                half3 n  = normalize(tn.x*i.t0 + tn.y*i.t1 + tn.z*i.t2);

                // Main light
                Light mainL = GetMainLight();
                half3 Ld    = normalize(mainL.direction);
                half  NdL   = saturate(dot(n, Ld));
                half  lightTerm = max(NdL, _MinLight);

                half3 diffuse  = albedo * mainL.color.rgb * lightTerm;

                half3 V = normalize(_WorldSpaceCameraPos - i.worldPos);
                half3 H = normalize(V + Ld);
                half  specPow = pow(saturate(dot(n, H)), _Shininess * 128.0);
                half3 specular = _SpecColor.rgb * specPow * mainL.color.rgb * NdL;

                // Additional lights
                uint addCount = GetAdditionalLightsCount();
                for (uint li = 0; li < addCount; ++li)
                {
                    Light addL = GetAdditionalLight(li, i.worldPos);
                    half3 Ldir = normalize(addL.direction);
                    half  Ndot = saturate(dot(n, Ldir));
                    if (Ndot <= 0) continue;

                    half3 attenColor = addL.color.rgb *
                                       addL.distanceAttenuation *
                                       addL.shadowAttenuation;

                    half  lTerm = max(Ndot, _MinLight);

                    diffuse  += albedo * attenColor * lTerm;

                    half3 H2 = normalize(V + Ldir);
                    half  sp = pow(saturate(dot(n, H2)), _Shininess * 128.0);
                    specular += _SpecColor.rgb * sp * attenColor * Ndot;
                }

                return half4(diffuse + specular, tex.a * i.color.a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
