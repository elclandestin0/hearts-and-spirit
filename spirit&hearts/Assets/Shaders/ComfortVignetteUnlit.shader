Shader "VR/ComfortVignetteUnlit"
{
    Properties
    {
        _Color("Vignette Color", Color) = (0,0,0,1)
        _Intensity("Intensity", Range(0,1)) = 0
        _Radius("Radius", Range(0,1)) = 0.9
        _Softness("Softness", Range(0.001,0.5)) = 0.2
        _Center("Center (UV)", Vector) = (0.5,0.5,0,0)
        _Aspect("Aspect (x/y)", Float) = 1.0
        _Ellipticity("Ellipticity (y scale)", Float) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Intensity;
            float _Radius;
            float _Softness;
            float4 _Center;
            float _Aspect;
            float _Ellipticity;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Normalize to [0..1], correct for aspect, center offset
                float2 uv = i.uv;
                float2 c = _Center.xy;
                float2 p = uv - c;

                // Make the circle look round on non-square aspect
                p.x *= _Aspect;
                p.y *= _Ellipticity;

                float d = length(p); // distance from center in normalized space

                // Soft edge: 0 inside radius, 1 at radius+softness
                float edge0 = _Radius;
                float edge1 = _Radius + _Softness;
                float mask = smoothstep(edge0, edge1, d);

                // Intensity drives alpha; color comes from _Color
                float alpha = saturate(mask * _Intensity);
                return fixed4(_Color.rgb, alpha);
            }
            ENDCG
        }
    }
}
