#ifndef SLUG_WIND_INCLUDED
#define SLUG_WIND_INCLUDED

// Island wind for plants, palms, grass and trees (docs/ideas/wind_ideas.md, NatureMeshKit.cs).
// Vertex colours painted by NatureMeshKit:
//   R = main bend weight (0 at root -> 1 at tip)
//   G = leaf-tip flutter weight
//   B = per-part phase offset (0..1)
// Material parameter:
//   _WindAmount = metres of sway at R=1 in a normal breeze (0 = does not move)
// Global uniforms set every frame by WeatherPresenter (all zero = off in preview scenes):
//   _SlugWind = (windDirX, windDirZ, windStrength 0..1, windTime)
//   _SlugGust = (gustWaveFreq, gustWaveSpeed, gustStrength, flutterSpeed)
float4 _SlugWind;
float4 _SlugGust;

void SlugWind(inout float3 posOS, inout float3 nrmOS, half4 vcol, half windAmount)
{
    float wAmt = windAmount * _SlugWind.z;
    if (wAmt <= 1e-4 || (vcol.r + vcol.g) <= 1e-4) return;

    float3 posWS = TransformObjectToWorld(posOS);
    float2 dir = _SlugWind.xy;
    float dirLen = max(length(dir), 1e-4);
    float2 windDir = dir / dirLen;
    float t = _SlugWind.w;
    float phase = vcol.b * 6.2831853;

    // Travelling gust wave across the island (meadows and tree crowns ripple in waves)
    float along = dot(posWS.xz, windDir);
    float crossW = posWS.x * windDir.y - posWS.z * windDir.x;
    float wavePhase = along * _SlugGust.x - t * _SlugGust.y + sin(crossW * 0.08 + phase) * 1.4;
    float gustWave = pow(0.5 + 0.5 * sin(wavePhase), 2.2);
    float gust = 1.0 + _SlugGust.z * gustWave;

    // Slow primary sway + gentle secondary bob
    float sway = (0.65 + 0.35 * sin(t * 1.9 + phase + along * 0.12)) * gust;
    float sideSway = 0.22 * cos(t * 1.3 + phase * 1.7 + crossW * 0.15);

    float bend = vcol.r * wAmt * sway;
    float2 sideDir = float2(-windDir.y, windDir.x);
    float3 dispWS = float3(
        (windDir.x * bend) + sideDir.x * (vcol.r * wAmt * sideSway),
        -abs(bend) * 0.22,
        (windDir.y * bend) + sideDir.y * (vcol.r * wAmt * sideSway)
    );

    // Fine leaf-tip flutter
    if (vcol.g > 1e-3)
    {
        float flt = sin(t * _SlugGust.w + phase * 3.1 + dot(posOS, float3(11.0, 17.0, 13.0)));
        float fltAmp = vcol.g * wAmt * 0.32 * (0.6 + 0.6 * gustWave);
        dispWS += float3(sideDir.x * flt * fltAmp, cos(t * (_SlugGust.w * 1.15) + phase) * fltAmp * 0.6, sideDir.y * flt * fltAmp);
    }

    posOS += TransformWorldToObjectDir(dispWS);
    float3 nrmWS = normalize(TransformObjectToWorldNormal(nrmOS) + float3(dispWS.x, 0.0, dispWS.z) * 0.45);
    nrmOS = normalize(TransformWorldToObjectNormal(nrmWS));
}

#endif
