Shader "Custom/FlatShadingAdvancedWithEmission"
{
    Properties
    {
        _Color ("Base Color", Color) = (1,1,1,1)
        _ShadowColor ("Shadow Color", Color) = (0.1, 0.1, 0.1, 1)
        _LightIntensity ("Light Intensity", Range(0, 2)) = 1
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0.3
        _SmoothShading ("Shading Softness", Range(0, 1)) = 0.1

        _FresnelPower ("Fresnel Focus (edge vs center)", Range(0.1, 5)) = 2
        _FresnelIntensity ("Fresnel Brightness", Range(0, 5)) = 1
        _FresnelColor ("Fresnel Color", Color) = (1,1,1,1)

        _SpecularIntensity ("Specular Intensity", Range(0, 2)) = 0.5
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _Metallic ("Metallic", Range(0, 1)) = 0.0

        // Nueva propiedad para la máscara de alfa
        _EmissionMask ("Emission Mask", 2D) = "white" { }
        _EmissionColor ("Emission Color", Color) = (1, 1, 1, 1)
        _EmissionIntensity ("Emission Intensity", Range(0, 2)) = 1
        _EmissionFresnelPower ("Emission Fresnel Power", Range(0.1, 5)) = 2
        _EmissionFresnelIntensity ("Emission Fresnel Intensity", Range(0, 5)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            fixed4 _Color;
            fixed4 _ShadowColor;
            float _LightIntensity;
            float _ShadowStrength;
            float _SmoothShading;

            float _FresnelPower;
            float _FresnelIntensity;
            fixed4 _FresnelColor;

            float _SpecularIntensity;
            fixed4 _SpecularColor;
            float _Metallic;

            // Propiedades de la emisión
            sampler2D _EmissionMask;
            fixed4 _EmissionColor;
            float _EmissionIntensity;
            float _EmissionFresnelPower;
            float _EmissionFresnelIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = worldPos.xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.color.xy; // Usamos coordenadas de color como UV
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 reflectDir = reflect(-lightDir, normal);

                // Cálculo de la luz difusa
                float NdotL = saturate(dot(normal, lightDir));
                float shadow = smoothstep(_ShadowStrength, _ShadowStrength + _SmoothShading, NdotL);
                float3 litColor = lerp(_ShadowColor.rgb, _Color.rgb, shadow) * _LightIntensity;

                // Fresnel
                float fresnel = pow(1.0 - saturate(dot(viewDir, normal)), _FresnelPower);
                float3 fresnelColor = _FresnelColor.rgb * fresnel * _FresnelIntensity;

                // Specular highlight with color
                float spec = pow(saturate(dot(viewDir, reflectDir)), 32.0) * _SpecularIntensity;
                float3 specularColor = _SpecularColor.rgb * spec;

                // Control de emisión con la máscara
                float mask = tex2D(_EmissionMask, i.uv).r; // Obtiene el valor de la máscara de emisión
                float emissionFresnel = pow(1.0 - saturate(dot(viewDir, normal)), _EmissionFresnelPower);
                float3 emission = _EmissionColor.rgb * mask * _EmissionIntensity * emissionFresnel * _EmissionFresnelIntensity;

                // Combina el color final
                float3 finalColor = litColor + fresnelColor + specularColor + emission;

                // Apply metallic influence (optional: multiply base color)
                finalColor = lerp(finalColor, finalColor * _Color.rgb, _Metallic);

                return fixed4(finalColor, 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
