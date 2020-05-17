Shader "Demo/Lit"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _NormalTex("Normal Map", 2D) = "bump" {}
        _NormalIntensity("Normal Map Intensity", Range(0,2)) = 1
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        [Gamma]_Metallic ("Metallic", Range(0,1)) = 0.0
    }

    CGINCLUDE

    #include "UnityPBSLighting.cginc"
    
    sampler2D _MainTex;
    sampler2D _NormalTex;
    float _NormalIntensity;
    float _ERROR;
        
    struct Input
    {
        float2 uv_MainTex;
        float4 screenPos;
        float3 worldPos;
        float3 worldNormal;
        INTERNAL_DATA
    };
    
    half _Glossiness;
    half _Metallic;
    fixed4 _Color;

    UNITY_INSTANCING_BUFFER_START(Props)

    UNITY_INSTANCING_BUFFER_END(Props)

    /*void vert(inout appdata_full v, out Input o)
    {
        UNITY_INITIALIZE_OUTPUT(Input, o);
        //o.PositionCS = UnityObjectToClipPos(v.vertex);
        o.worldPos = mul(unity_ObjectToWorld, v.vertex);
        //o.NormalWS = UnityObjectToWorldNormal(v.normal);
    }*/

    inline half4 LightingCustomLitGI(SurfaceOutputStandard s,half3 viewDir,UnityGI gi)
    {
        return LightingStandard(s, viewDir, gi);
    }

    inline void LightingCustomLitGI_GI(
        SurfaceOutputStandard s,
        UnityGIInput data,
        inout UnityGI gi)
    {
        LightingStandard_GI(s, data, gi);
    }

#if (defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3)) && defined(_ADDITIONAL_DECALS)
#include "Assets/GPUDecals/Shaders/Decals.cginc"
#endif

    void surf(Input IN, inout SurfaceOutputStandard o)
    {
        fixed4 col = tex2D(_MainTex, IN.uv_MainTex) * _Color;
        float3 normalTS = UnpackNormal(tex2D(_NormalTex, IN.uv_MainTex));
#if (defined(SHADER_API_D3D11) || defined(SHADER_API_GLES3)) && defined(_ADDITIONAL_DECALS)
        DecalInput input;
        input.PositionWS = IN.worldPos;
        input.NormalWS = WorldNormalVector(IN, o.Normal).xyz;
        input.PositionCS = IN.screenPos;
        AdditionalDetal(input, col.rgb, normalTS);
#else
        col.rgb += (IN.worldPos.xyz + WorldNormalVector(IN, o.Normal).xyz + IN.screenPos.xyz) * _ERROR;
#endif
        o.Albedo = col.rgb;
        o.Normal = float3(normalTS.xy * _NormalIntensity, normalTS.z);
        o.Metallic = _Metallic;
        o.Smoothness = _Glossiness;
        o.Alpha = col.a;
    }

    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf CustomLitGI fullforwardshadows /*vertex:vert*/
        #pragma target 4.5
        #pragma multi_compile _ _ADDITIONAL_DECALS
        #pragma multi_compile _ _ADDITIONAL_LIGHTS
        #pragma multi_compile _ _CULLING_CLUSTER_ON
        #pragma multi_compile _ _STRUCTURED_BUFFER_SUPPORT
        ENDCG
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf CustomLitGI fullforwardshadows /*vertex:vert*/
        #pragma target 3.0
        #pragma multi_compile _ _ADDITIONAL_DECALS
        #pragma multi_compile _ _ADDITIONAL_LIGHTS
        ENDCG
    }

    FallBack "Diffuse"
}
