Shader "Hidden/Edge Detection With Stencil"
{
    Properties
    {
        _NormalThreshold ("Normal Threshold", Float) = 1
        _DepthThreshold ("Depth Threshold", Float) = 1
        _LuminanceThreshold ("Luminance Threshold", Float) = 1
        _OutlineThickness ("Outline Thickness", Float) = 1
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _WiggleFrequency ("Wiggle Frequency", Float) = 0.08
        _WiggleAmplitude ("Wiggle Amplitude", Float) = 2.0
        
        // New properties for distance-based adaptation
        _DistanceScale ("Distance Scale Factor", Float) = 0.1
        _MaxThickness ("Max Outline Thickness", Float) = 3.0
        _MinThickness ("Min Outline Thickness", Float) = 0.5
        _ThresholdScale ("Threshold Distance Scale", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"="Opaque"
        }

        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        
        // Stencil test - only process pixels that DON'T have stencil value 1
        Stencil
        {
            Ref 1
            Comp NotEqual
            ReadMask 255
        }

        Pass 
        {
            Name "EDGE DETECTION OUTLINE"
            
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            float _NormalThreshold;
            float _DepthThreshold;
            float _LuminanceThreshold;
            float _OutlineThickness;
            float4 _OutlineColor;
            float _WiggleFrequency;
            float _WiggleAmplitude;
            
            // New distance-adaptive parameters
            float _DistanceScale;
            float _MaxThickness;
            float _MinThickness;
            float _ThresholdScale;

            #pragma vertex Vert
            #pragma fragment frag

            // Hash function for randomness
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.x, p.y, p.x) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // Generate displacement for hand-drawn effect
            float2 generateDisplacement(float2 screenPos, float2 texelSize)
            {
                float hashValue = hash(floor(screenPos * 0.1));
                float2 displacement = float2(
                    hashValue * sin(screenPos.y * _WiggleFrequency),
                    hashValue * cos(screenPos.x * _WiggleFrequency)
                ) * _WiggleAmplitude * texelSize * 0.5;
                
                return displacement;
            }

            // Distance-adaptive thickness calculation
            float calculateAdaptiveThickness(float depth)
            {
                // Convert depth to linear world distance
                float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
                
                // Scale thickness inversely with distance
                float distanceFactor = 1.0 / (1.0 + linearDepth * _DistanceScale);
                float adaptiveThickness = lerp(_MaxThickness, _MinThickness, distanceFactor);
                
                return adaptiveThickness * _OutlineThickness;
            }
            
            // Distance-adaptive threshold calculation
            float3 calculateAdaptiveThresholds(float depth)
            {
                float linearDepth = LinearEyeDepth(depth, _ZBufferParams);
                float distanceFactor = 1.0 + linearDepth * _ThresholdScale;
                
                return float3(
                    _NormalThreshold / distanceFactor,
                    _DepthThreshold / distanceFactor,
                    _LuminanceThreshold / distanceFactor
                );
            }

            // Multi-sample edge detection for better quality at distance
            float SobelEdgeDetection(float samples[9])
            {
                // Sobel X kernel: [-1, 0, 1; -2, 0, 2; -1, 0, 1]
                float sobelX = -samples[0] + samples[2] - 2*samples[3] + 2*samples[5] - samples[6] + samples[8];
                
                // Sobel Y kernel: [-1, -2, -1; 0, 0, 0; 1, 2, 1]
                float sobelY = -samples[0] - 2*samples[1] - samples[2] + samples[6] + 2*samples[7] + samples[8];
                
                return sqrt(sobelX * sobelX + sobelY * sobelY);
            }

            // Roberts Cross edge detection (original method)
            float RobertsCross(float3 samples[4])
            {
                const float3 difference_1 = samples[1] - samples[2];
                const float3 difference_2 = samples[0] - samples[3];
                return sqrt(dot(difference_1, difference_1) + dot(difference_2, difference_2));
            }

            float RobertsCross(float samples[4])
            {
                const float difference_1 = samples[1] - samples[2];
                const float difference_2 = samples[0] - samples[3];
                return sqrt(difference_1 * difference_1 + difference_2 * difference_2);
            }
            
            float3 SampleSceneNormalsRemapped(float2 uv)
            {
                return SampleSceneNormals(uv) * 0.5 + 0.5;
            }

            float SampleSceneLuminance(float2 uv)
            {
                float3 color = SampleSceneColor(uv);
                return color.r * 0.3 + color.g * 0.59 + color.b * 0.11;
            }

            half4 frag(Varyings IN) : SV_TARGET
            {
                float2 uv = IN.texcoord;
                float2 texel_size = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);
                
                // Sample depth at center to determine distance
                float centerDepth = SampleSceneDepth(uv);
                
                // Calculate distance-adaptive parameters
                float adaptiveThickness = calculateAdaptiveThickness(centerDepth);
                float3 adaptiveThresholds = calculateAdaptiveThresholds(centerDepth);
                
                // Get world position for displacement
                float3 worldPos = ComputeWorldSpacePosition(uv, centerDepth, UNITY_MATRIX_I_VP);
                float2 baseDisplacement = generateDisplacement(worldPos.xz * 10.0, texel_size);
                
                // Use adaptive thickness for sampling
                const float half_width_f = floor(adaptiveThickness * 0.5);
                const float half_width_c = ceil(adaptiveThickness * 0.5);

                float2 uvs[4];
                uvs[0] = uv + baseDisplacement + texel_size * float2(half_width_f, half_width_c) * float2(-1, 1);
                uvs[1] = uv + baseDisplacement + texel_size * float2(half_width_c, half_width_c) * float2(1, 1);
                uvs[2] = uv + baseDisplacement + texel_size * float2(half_width_f, half_width_f) * float2(-1, -1);
                uvs[3] = uv + baseDisplacement + texel_size * float2(half_width_c, half_width_f) * float2(1, -1);
                
                float3 normal_samples[4];
                float depth_samples[4], luminance_samples[4];
                
                [unroll]for (int i = 0; i < 4; i++) {
                    depth_samples[i] = SampleSceneDepth(uvs[i]);
                    normal_samples[i] = SampleSceneNormalsRemapped(uvs[i]);
                    luminance_samples[i] = SampleSceneLuminance(uvs[i]);
                }
                
                // Apply edge detection
                float edge_depth = RobertsCross(depth_samples);
                float edge_normal = RobertsCross(normal_samples);
                float edge_luminance = RobertsCross(luminance_samples);
                
                // Use adaptive thresholds
                edge_depth = edge_depth > adaptiveThresholds.y ? 1 : 0;
                edge_normal = edge_normal > adaptiveThresholds.x ? 1 : 0;
                edge_luminance = edge_luminance > adaptiveThresholds.z ? 1 : 0;
                
                // Combine edges
                float edge = max(edge_depth, max(edge_normal, edge_luminance));
                
                // Additional distance-based edge thinning
                float linearDepth = LinearEyeDepth(centerDepth, _ZBufferParams);
                float edgeAttenuation = saturate(1.0 - linearDepth * _DistanceScale * 0.1);
                edge *= edgeAttenuation;
                
                return edge * _OutlineColor;
            }
            ENDHLSL
        }
    }
}