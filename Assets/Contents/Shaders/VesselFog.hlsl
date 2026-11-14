#ifndef VESSEL_FOG_INCLUDED
#define VESSEL_FOG_INCLUDED

// Distance fog shared by the vessel wall and blood cells, so depth fades to the same dark red.
half3 ApplyVesselFog(half3 color, float3 positionWS, half3 fogColor, float fogStart, float fogEnd)
{
    float distance = length(positionWS - GetCameraPositionWS());
    float fog = saturate((distance - fogStart) / max(fogEnd - fogStart, 0.001));
    return lerp(color, fogColor, fog * fog);
}

float VesselHash(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float VesselNoise(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(
        lerp(lerp(VesselHash(i), VesselHash(i + float3(1, 0, 0)), f.x),
             lerp(VesselHash(i + float3(0, 1, 0)), VesselHash(i + float3(1, 1, 0)), f.x), f.y),
        lerp(lerp(VesselHash(i + float3(0, 0, 1)), VesselHash(i + float3(1, 0, 1)), f.x),
             lerp(VesselHash(i + float3(0, 1, 1)), VesselHash(i + float3(1, 1, 1)), f.x), f.y),
        f.z);
}

#endif
