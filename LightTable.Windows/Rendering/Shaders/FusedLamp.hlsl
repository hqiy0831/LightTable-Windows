#define D2D_INPUT_COUNT 4
#define D2D_INPUT0_SIMPLE
#define D2D_INPUT1_SIMPLE
#define D2D_INPUT2_SIMPLE
#define D2D_INPUT3_SIMPLE
#include "d2d1effecthelpers.hlsli"

// controls: boosted mix, ignition halo mix, level, linear lamp gain.
float4 controls;
// lighting: highlight exposure stops, transmission.
float2 lighting;

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
    float4 lit = D2DGetInput(0);
    float4 boosted = D2DGetInput(1);
    float4 halo = D2DGetInput(2);
    float4 dark = D2DGetInput(3);
    float4 mixed = saturate(lerp(lit, boosted, controls.x));
    // Porter-Duff Screen, expressed directly on premultiplied channels.
    float4 screened = saturate(mixed + halo - mixed * halo);
    float4 color = saturate(lerp(mixed, screened, controls.y));
    color.rgb = saturate(color.rgb * controls.z);
    if (controls.w > 1.0 || lighting.x > 0.0)
    {
        float3 result = toLinear(color.a > 0.0 ? color.rgb / color.a : 0.0);
        if (controls.w > 1.0) result = shoulder(result, controls.w, 0.80, 0.20);
        if (lighting.x > 0.0)
        {
            float light = dot(result, float3(0.2126, 0.7152, 0.0722));
            result = shoulder(result, exp2(lighting.x * smoothstep(0.004, 0.075, light) * smoothstep(0.40, 0.88, light)), 0.85, 0.15);
        }
        color.rgb = toSrgb(result) * color.a;
    }
    return saturate(lerp(dark, color, lighting.y));
}
