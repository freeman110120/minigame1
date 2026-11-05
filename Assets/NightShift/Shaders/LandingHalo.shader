Shader "NightShift/Landing Halo"
{
    Properties
    {
        _Color ("Halo Color", Color) = (1, 0.55, 0.23, 0.40)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct Vertex { float4 position : POSITION; float2 uv : TEXCOORD0; };
            struct Varying { float4 position : SV_POSITION; float edge : TEXCOORD0; };
            fixed4 _Color;

            Varying vert(Vertex input)
            {
                Varying output;
                output.position = UnityObjectToClipPos(input.position);
                output.edge = input.uv.y;
                return output;
            }

            fixed4 frag(Varying input) : SV_Target
            {
                half fade = saturate(1.0h - abs(input.edge * 2.0h - 1.0h));
                return fixed4(_Color.rgb, _Color.a * fade * fade);
            }
            ENDCG
        }
    }
}
