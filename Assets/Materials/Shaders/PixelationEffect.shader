Shader "Custom/PixelationEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PixelSize ("Pixel Size", Range(1, 100)) = 10
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            Name "PixelationPass"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _PixelSize;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                // Get screen dimensions
                float2 screenSize = _ScreenParams.xy;
                
                // Calculate pixel grid
                float2 pixelGrid = floor(input.uv * screenSize / _PixelSize) * _PixelSize;
                float2 pixelatedUV = pixelGrid / screenSize;
                
                // Sample the texture at the pixelated UV
                float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, pixelatedUV);
                
                return color;
            }
            ENDHLSL
        }
    }
}