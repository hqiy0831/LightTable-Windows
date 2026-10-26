#define D2D_INPUT_COUNT 1
#define D2D_INPUT0_COMPLEX
#define D2D_REQUIRES_SCENE_POSITION
#include "d2d1effecthelpers.hlsli"

float2 center;
float radius;
float strength;

D2D_PS_ENTRY(main)
{
    float2 delta = D2DGetScenePosition().xy - center;
    float distance = length(delta);
    float r = distance / max(radius, 0.001);
    float shoulder = smoothstep(0.75, 1.0, r);
    float lip = smoothstep(0.94, 1.0, r);
    float inset = radius * (0.028 * shoulder + 0.004 * lip) * strength;
    float2 samplePoint = center + delta * (1.0 - inset / max(distance, 0.001));
    return D2DSampleInputAtPosition(0, samplePoint);
}
