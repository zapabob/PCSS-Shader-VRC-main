#ifndef PCSS_INCLUDED
#define PCSS_INCLUDED

#include "UnityCG.cginc"

// ポアソンディスク分布のサンプル点
static const float2 PoissonDisk[64] = {
    float2(-0.5119625f, -0.4827938f),
    float2(-0.2171264f, -0.4768726f),
    float2(0.12074f, -0.4754578f),
    float2(0.4159f, -0.4520434f),
    float2(-0.4728346f, -0.1096018f),
    float2(-0.1922931f, -0.1092816f),
    float2(0.08989531f, -0.1089613f),
    float2(0.3719638f, -0.1086411f),
    float2(-0.4337072f, 0.2635818f),
    float2(-0.1674598f, 0.2639021f),
    float2(0.0987877f, 0.2642223f),
    float2(0.3650352f, 0.2645425f),
    float2(-0.3945799f, 0.6367654f),
    float2(-0.1426264f, 0.6370856f),
    float2(0.1093271f, 0.6374058f),
    float2(0.3612806f, 0.637726f),
    float2(-0.7071068f, -0.7071068f),
    float2(-0.7071068f, 0.0f),
    float2(-0.7071068f, 0.7071068f),
    float2(0.0f, -0.7071068f),
    float2(0.0f, 0.0f),
    float2(0.0f, 0.7071068f),
    float2(0.7071068f, -0.7071068f),
    float2(0.7071068f, 0.0f),
    float2(0.7071068f, 0.7071068f),
    float2(-0.8660254f, -0.5f),
    float2(-0.8660254f, 0.5f),
    float2(0.0f, -1.0f),
    float2(0.0f, 1.0f),
    float2(0.8660254f, -0.5f),
    float2(0.8660254f, 0.5f),
    float2(-0.9396926f, -0.3420201f),
    float2(-0.9396926f, 0.3420201f),
    float2(-0.7660444f, -0.6427876f),
    float2(-0.7660444f, 0.6427876f),
    float2(-0.5f, -0.8660254f),
    float2(-0.5f, 0.8660254f),
    float2(-0.1736482f, -0.9848078f),
    float2(-0.1736482f, 0.9848078f),
    float2(0.1736482f, -0.9848078f),
    float2(0.1736482f, 0.9848078f),
    float2(0.5f, -0.8660254f),
    float2(0.5f, 0.8660254f),
    float2(0.7660444f, -0.6427876f),
    float2(0.7660444f, 0.6427876f),
    float2(0.9396926f, -0.3420201f),
    float2(0.9396926f, 0.3420201f),
    float2(-0.9876883f, -0.1564345f),
    float2(-0.9876883f, 0.1564345f),
    float2(-0.8910065f, -0.4539905f),
    float2(-0.8910065f, 0.4539905f),
    float2(-0.7071068f, -0.7071068f),
    float2(-0.7071068f, 0.7071068f),
    float2(-0.4539905f, -0.8910065f),
    float2(-0.4539905f, 0.8910065f),
    float2(-0.1564345f, -0.9876883f),
    float2(-0.1564345f, 0.9876883f),
    float2(0.1564345f, -0.9876883f),
    float2(0.1564345f, 0.9876883f),
    float2(0.4539905f, -0.8910065f),
    float2(0.4539905f, 0.8910065f),
    float2(0.7071068f, -0.7071068f),
    float2(0.7071068f, 0.7071068f),
    float2(0.8910065f, -0.4539905f),
    float2(0.8910065f, 0.4539905f)
};

float2 GetRotatedPoissonDisk(float2 poissonDisk, float2 uv, sampler2D noiseTexture, float4 noiseCoords)
{
    float angle = tex2D(noiseTexture, uv * noiseCoords.xy).r * 3.14159265359 * 2.0;
    float s = sin(angle);
    float c = cos(angle);
    float2x2 rotationMatrix = float2x2(c, -s, s, c);
    return mul(rotationMatrix, poissonDisk);
}

float FindBlockerDistance(float2 uv, float currentDepth, int blockerSampleCount, float searchRadius, 
    float maxStaticBias, float blockerBias, sampler2D shadowMap, sampler2D noiseTexture, float4 noiseCoords)
{
    float blockerSum = 0.0;
    float blockerCount = 0.0;
    float bias = maxStaticBias + blockerBias * currentDepth;

    for(int i = 0; i < blockerSampleCount; i++)
    {
        float2 offset = GetRotatedPoissonDisk(PoissonDisk[i], uv, noiseTexture, noiseCoords) * searchRadius;
        float shadowMapDepth = tex2D(shadowMap, uv + offset).r;
        
        if(shadowMapDepth < currentDepth - bias)
        {
            blockerSum += shadowMapDepth;
            blockerCount += 1.0;
        }
    }

    return blockerCount > 0.0 ? blockerSum / blockerCount : -1.0;
}

float PCF_Filter(float2 uv, float currentDepth, float filterRadius, int pcfSampleCount,
    float maxStaticBias, float pcfBias, sampler2D shadowMap, sampler2D noiseTexture, float4 noiseCoords)
{
    float sum = 0.0;
    float bias = maxStaticBias + pcfBias * currentDepth;

    for(int i = 0; i < pcfSampleCount; i++)
    {
        float2 offset = GetRotatedPoissonDisk(PoissonDisk[i], uv, noiseTexture, noiseCoords) * filterRadius;
        float shadowMapDepth = tex2D(shadowMap, uv + offset).r;
        sum += shadowMapDepth > currentDepth - bias ? 1.0 : 0.0;
    }

    return sum / float(pcfSampleCount);
}

float CalculatePCSSShadow(float4 worldPos, int blockerSampleCount, int pcfSampleCount,
    float softness, float sampleRadius, float maxStaticBias, float blockerBias, float pcfBias,
    float cascadeBlendDistance, sampler2D noiseTexture, float4 noiseCoords)
{
    float4 shadowCoord = mul(unity_WorldToShadow[0], worldPos);
    float2 uv = shadowCoord.xy;
    float currentDepth = shadowCoord.z;

    // ブロッカー検索
    float searchRadius = sampleRadius * (1.0 - currentDepth);
    float blockerDistance = FindBlockerDistance(uv, currentDepth, blockerSampleCount, searchRadius,
        maxStaticBias, blockerBias, _ShadowMapTexture, noiseTexture, noiseCoords);

    // ブロッカーが見つからない場合は完全に明るい
    if(blockerDistance < 0.0)
        return 1.0;

    // ペナンブラサイズの計算
    float penumbraSize = (currentDepth - blockerDistance) * softness / blockerDistance;
    
    // PCFフィルタリング
    float filterRadius = penumbraSize * sampleRadius;
    float shadow = PCF_Filter(uv, currentDepth, filterRadius, pcfSampleCount,
        maxStaticBias, pcfBias, _ShadowMapTexture, noiseTexture, noiseCoords);

    // カスケードブレンド
    if(cascadeBlendDistance > 0.0)
    {
        float cascadeEdge = 1.0 - max(abs(uv.x - 0.5), abs(uv.y - 0.5)) * 2.0;
        shadow = lerp(1.0, shadow, smoothstep(0.0, cascadeBlendDistance, cascadeEdge));
    }

    return shadow;
}

#endif // PCSS_INCLUDED 