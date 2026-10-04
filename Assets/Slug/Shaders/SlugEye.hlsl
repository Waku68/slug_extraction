#ifndef SLUG_EYE_INCLUDED
#define SLUG_EYE_INCLUDED

// Procedural 3D-eyeball surface for Slug/Toon (_EyeKind > 0).
// The eye is a mesh baked in body space; this function paints iris / pupil / sclera in the eye's own frame:
//   q = (x outward, y up) / radius,  rr = 0 at the front centre of the eyeball.
// Needs SlugToonInput.hlsl (properties, ValueNoise3, SlugTime).

half3 EyeSurface(float3 posOS, out float glint)
{
    float3 d = posOS - _EyeFC.xyz;
    float R = max(_EyeRad, 1e-4);
    float2 q = float2(dot(d, _EyeFX.xyz), dot(d, _EyeFY.xyz)) / R;
    float rr = length(q);
    float ang = atan2(q.y, q.x);
    float kind = _EyeKind;
    half3 dark = half3(0.02, 0.018, 0.02);
    half3 col = half3(0, 0, 0);
    float pupil = 0.0;

    if (kind < 1.5)
    {
        // 1: reptile - vertical slit pupil, finely streaked iris
        float e = length(float2(q.x / 0.13, q.y / 0.74));
        pupil = 1.0 - smoothstep(0.9, 1.0, e);
        float t01 = saturate((rr - 0.1) / 0.75);
        float fib = 0.5 + 0.5 * sin(ang * 36.0 + ValueNoise3(posOS * 110.0) * 7.0);
        float fine = ValueNoise3(float3(rr * 22.0, ang * 4.0, 0.7));
        col = lerp(_EyeIris1.rgb, _EyeIris2.rgb, t01);
        col *= 0.72 + 0.38 * fib * (1.0 - 0.35 * t01) + 0.14 * (fine - 0.5);
        col = lerp(col, col * 0.55, smoothstep(0.7, 0.0, e * 0.35));   // shadow around the slit
        col = lerp(col, dark, smoothstep(0.82, 0.95, rr));              // dark limbus
    }
    else if (kind < 2.5)
    {
        // 2: frog - horizontal pupil, gold iris with dark reticulation
        float e = length(float2(q.x / 0.56, q.y / 0.25));
        pupil = 1.0 - smoothstep(0.9, 1.0, e);
        float t01 = saturate((rr - 0.2) / 0.7);
        col = lerp(_EyeIris1.rgb, _EyeIris2.rgb, t01 * 0.7);
        float n = ValueNoise3(float3(q * 8.0, 1.3));
        float vein = 1.0 - smoothstep(0.0, 0.07, abs(n - 0.5));
        float speck = smoothstep(0.7, 0.82, ValueNoise3(float3(q * 22.0, 4.1)));
        col = lerp(col, dark * 3.0, saturate(vein * 0.65 + speck * 0.5) * smoothstep(0.22, 0.55, rr));
        col *= 1.0 + 0.25 * smoothstep(0.5, 0.2, rr) * smoothstep(0.1, 0.4, abs(q.x));   // golden glow beside the pupil
        col = lerp(col, dark, smoothstep(0.84, 0.97, rr));
    }
    else if (kind < 3.5)
    {
        // 3: octopus - a long horizontal band for a pupil, amber iris
        float2 bx = float2(0.78, 0.15);
        float2 dd = abs(q) - bx + 0.15;
        float sd = length(max(dd, 0.0)) + min(max(dd.x, dd.y), 0.0) - 0.15;
        pupil = 1.0 - smoothstep(-0.02, 0.05, sd);
        float t01 = saturate((rr - 0.1) / 0.8);
        col = lerp(_EyeIris1.rgb, _EyeIris2.rgb, t01);
        col *= 0.88 + 0.1 * sin(q.x * 26.0 + ValueNoise3(posOS * 60.0) * 6.0) + 0.16 * (ValueNoise3(posOS * 75.0) - 0.5);
        col = lerp(col, dark, smoothstep(0.8, 0.95, rr));
    }
    else if (kind < 4.5)
    {
        // 4: fish - silvery bulging ball, bright iris ring around a round pupil
        col = _EyeSclera.rgb * (0.9 + 0.12 * ValueNoise3(posOS * 60.0));
        float ring = smoothstep(0.62, 0.58, rr) * smoothstep(0.26, 0.32, rr);
        half3 iris = lerp(_EyeIris2.rgb, _EyeIris1.rgb, smoothstep(0.3, 0.52, rr));
        iris *= 0.94 + 0.08 * sin(ang * 22.0 + ValueNoise3(posOS * 90.0) * 5.0) + 0.12 * (ValueNoise3(posOS * 120.0) - 0.5);
        col = lerp(col, iris, ring);
        col = lerp(col, dark * 2.0, smoothstep(0.55, 0.6, rr) * smoothstep(0.7, 0.64, rr));   // outline of the iris
        pupil = 1.0 - smoothstep(0.27, 0.3, rr);
    }
    else if (kind < 5.5)
    {
        // 5: nictitating membrane - round pupil, golden iris, milky membrane that closes slowly
        pupil = 1.0 - smoothstep(0.28, 0.32, rr);
        float t01 = saturate((rr - 0.15) / 0.7);
        float fib = 0.5 + 0.5 * sin(ang * 30.0 + ValueNoise3(posOS * 100.0) * 6.0);
        col = lerp(_EyeIris1.rgb, _EyeIris2.rgb, t01) * (0.78 + 0.3 * fib);
        col = lerp(col, dark, smoothstep(0.82, 0.95, rr));
        // the membrane sweeps from the inner corner (-x) to the outer one, then slowly opens again
        float p = fmod(SlugTime(), 5.0);
        float c = smoothstep(0.0, 1.6, p) * (1.0 - smoothstep(1.9, 3.5, p));
        float edge = -1.15 + 2.3 * c;
        float cover = (1.0 - smoothstep(edge - 0.05, edge + 0.05, q.x)) * step(0.001, c);
        float edgeGlow = exp(-pow((q.x - edge) / 0.06, 2.0)) * step(0.001, c);
        col = lerp(col, half3(0.82, 0.88, 0.9), 0.48 * cover);
        col = lerp(col, half3(0.95, 0.98, 1.0), 0.55 * edgeGlow);
    }
    else
    {
        // 6: a black bead (the lids are separate meshes)
        col = lerp(half3(0.015, 0.012, 0.014), _EyeIris2.rgb, smoothstep(0.55, 0.95, rr) * 0.6);
    }

    col = lerp(col, dark, pupil);
    // a small, restrained highlight
    glint = smoothstep(0.1, 0.06, length(q - float2(-0.34, 0.38)));
    return col;
}

#endif
