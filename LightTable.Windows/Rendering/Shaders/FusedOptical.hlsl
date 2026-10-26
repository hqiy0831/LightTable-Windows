#define D2D_INPUT_COUNT 1
#define D2D_INPUT0_SIMPLE
#include "d2d1effecthelpers.hlsli"

float lampGain;
float exposureStops;

float3 toLinear(float3 value)
{
    return float3(value.r <= 0.04045 ? value.r / 12.92 : pow((value.r + 0.055) / 1.055, 2.4),
                  value.g <= 0.04045 ? value.g / 12.92 : pow((value.g + 0.055) / 1.055, 2.4),
                  value.b <= 0.04045 ? value.b / 12.92 : pow((value.b + 0.055) / 1.055, 2.4));
}
float3 toSrgb(float3 value)
{
    return float3(value.r <= 0.0031308 ? value.r * 12.92 : 1.055 * pow(value.r, 1.0 / 2.4) - 0.055,
                  value.g <= 0.0031308 ? value.g * 12.92 : 1.055 * pow(value.g, 1.0 / 2.4) - 0.055,
                  value.b <= 0.0031308 ? value.b * 12.92 : 1.055 * pow(value.b, 1.0 / 2.4) - 0.055);
}
float3 shoulder(float3 value, float gain, float knee, float room)
{
    float sourcePeak = max(value.r, max(value.g, value.b));
    float peak = sourcePeak * gain;
    float excess = max(0.0, peak - knee);
    float compressed = knee + room * excess / (room + excess);
    float outputPeak = peak > knee ? max(sourcePeak, compressed) : peak;
    return value * (sourcePeak > 0.0 ? outputPeak / sourcePeak : gain);
}
D2D_PS_ENTRY(main)
{
    float4 color = D2DGetInput(0);
    if (lampGain <= 1.0 && exposureStops <= 0.0) return color;
    float3 result = toLinear(color.a > 0.0 ? color.rgb / color.a : 0.0);
    if (lampGain > 1.0) result = shoulder(result, lampGain, 0.80, 0.20);
    if (exposureStops > 0.0)
    {
        // WIC currently supplies sRGB primaries. P3 weights cannot be used
        // after converting/clipping source pixels to sRGB.
        float light = dot(result, float3(0.2126, 0.7152, 0.0722));
        float protection = smoothstep(0.004, 0.075, light);
        float highlight = smoothstep(0.40, 0.88, light);
        result = shoulder(result, exp2(exposureStops * protection * highlight), 0.85, 0.15);
    }
    return float4(toSrgb(result) * color.a, color.a);
}
