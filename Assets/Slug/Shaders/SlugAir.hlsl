#ifndef SLUG_AIR_INCLUDED
#define SLUG_AIR_INCLUDED

// The island's air, shared by Slug/Toon and the toon ground (Slug/ToonTerrain). WeatherPresenter sets these for the
// whole scene; all zero = off, so previews and other scenes see the plain toon.
float4 _SlugAir;          // x haze density (exp squared, per metre), y the most it hides (0 = all), w 1 = on
float4 _SlugAirColor;
float4 _SlugShadeTint;    // rgb multiplies every shade colour, a how much
float4 _SlugMist;         // x density at the base (per metre), y falloff (per metre of height), z base height, w 1 = on
float4 _SlugMistColor;
float4 _SlugGlow;         // rgb colour x strength, w height where it starts
float4 _SlugGlowRange;    // x metres from the start to full
float4 _SlugGroundRect;   // x, y: world x, z of the corner; z: 1 / size; w 1 = on
TEXTURE2D(_SlugGround);
SAMPLER(sampler_SlugGround);

float4 _SlugWet;          // x wet in sun (0..1), y wet in shade (0..1, 4x longer after rain), z scene rain (0..1), w 1 = on
float4 _SlugCloudShadow;  // xy wind drift offset, z shadow strength (0..1), w scale (1/m)
float4 _SlugRainCloud0;   // xy world center xz, z 1 / radius, w rain strength (0..1)
float4 _SlugRainCloud1;   // xy world center xz, z 1 / radius, w rain strength (0..1)

half3 SlugShade(half3 shade)
{
    return lerp(shade, shade * _SlugShadeTint.rgb, _SlugShadeTint.a);
}

// Local rain intensity (0..1) at a world point, including the 1-2 moving rain clouds overhead (branchless)
float SlugRainAt(float3 positionWS)
{
    float d0 = length(positionWS.xz - _SlugRainCloud0.xy) * _SlugRainCloud0.z;
    float d1 = length(positionWS.xz - _SlugRainCloud1.xy) * _SlugRainCloud1.z;
    float r0 = _SlugRainCloud0.w * (1.0 - smoothstep(0.55, 1.0, d0));
    float r1 = _SlugRainCloud1.w * (1.0 - smoothstep(0.55, 1.0, d1));
    return saturate(max(_SlugWet.z, max(r0, r1))) * step(0.5, _SlugWet.w);
}

// Dappled cloud shadows drifting with the wind + deep shade under the moving rain clouds
float SlugCloudShadowAt(float3 positionWS)
{
    float2 uv = positionWS.xz * _SlugCloudShadow.w + _SlugCloudShadow.xy;
    float w1 = sin(uv.x * 3.1 + cos(uv.y * 2.7)) * cos(uv.y * 3.3 - sin(uv.x * 2.3));
    float w2 = sin((uv.x * 0.8 - uv.y * 0.6) * 5.3 + 1.7);
    float dapple = smoothstep(0.15, 0.55, w1 * 0.65 + w2 * 0.35) * _SlugCloudShadow.z;

    float d0 = length(positionWS.xz - _SlugRainCloud0.xy) * _SlugRainCloud0.z;
    float d1 = length(positionWS.xz - _SlugRainCloud1.xy) * _SlugRainCloud1.z;
    float rc = max(_SlugRainCloud0.w * (1.0 - smoothstep(0.65, 1.18, d0)),
                   _SlugRainCloud1.w * (1.0 - smoothstep(0.65, 1.18, d1)));
    return 1.0 - saturate(max(dapple, rc * 0.78));
}

// Photographer's rain look (near): wet island ground, rocks and leaves turn deeper and richer in colour and catch a wet sheen.
void SlugWetSurface(inout half3 albedo, out half3 wetSheen, float3 positionWS, float3 normalWS, float3 viewDirWS,
                    float3 lightDirWS, half3 lightCol, float shadowAtten, float islandLook)
{
    float on = step(0.5, _SlugWet.w) * step(0.5, islandLook);
    float baseWet = lerp(_SlugWet.y, _SlugWet.x, smoothstep(0.25, 0.75, shadowAtten));
    float wet = max(baseWet, SlugRainAt(positionWS)) * on;

    half luma = dot(albedo, half3(0.299, 0.587, 0.114));
    half isSalt = smoothstep(0.75, 0.90, luma);
    half3 rich = saturate(lerp(half3(luma, luma, luma), albedo, 1.22)) * lerp(0.74, 0.90, isSalt);
    albedo = lerp(albedo, rich, wet);

    float upFace = smoothstep(0.15, 0.65, normalWS.y);
    float3 H = normalize(lightDirWS + viewDirWS);
    float ndh = saturate(dot(normalWS, H));
    float specStep = smoothstep(0.86, 0.93, ndh) * 0.55 + smoothstep(0.965, 0.985, ndh) * 0.45;
    float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 3.0) * upFace;
    half3 skySheenCol = lerp(lightCol, _SlugAirColor.rgb + half3(0.18, 0.22, 0.28), 0.6);
    wetSheen = skySheenCol * (wet * upFace * (specStep * lerp(0.35, 1.0, shadowAtten) * 0.38 + fresnel * 0.22));
}

// how much low mist lies between the eye and a point (height fog, integrated along the view ray)
float SlugMistAmount(float3 positionWS)
{
    if (_SlugMist.w < 0.5) return 0.0;
    float3 ro = _WorldSpaceCameraPos;
    float3 v = positionWS - ro;
    float len = length(v);
    float rdy = v.y / max(len, 1e-3);
    float b = _SlugMist.y;
    float h0 = clamp(ro.y - _SlugMist.z, -20.0, 400.0);
    float k = b * rdy * len;
    float path = abs(k) > 1e-3 ? (1.0 - exp(-k)) / (b * rdy) : len;
    float optical = _SlugMist.x * exp(-b * h0) * path;
    return 1.0 - exp(-max(optical, 0.0));
}

// Photographer's rain look (far): island surfaces (islandLook > 0.5) lose saturation with distance in the rain mist,
// while slugs and gear (islandLook < 0.5) keep their saturation so bright/vivid bodies stand out afar in heavy rain.
half3 SlugAirLook(half3 col, float3 positionWS, float islandLook)
{
    if (_SlugAir.w < 0.5) return col;
    float dist = distance(positionWS, _WorldSpaceCameraPos);
    float rain = SlugRainAt(positionWS);
    float farRain = smoothstep(10.0, 58.0, dist) * rain * 0.72 * step(0.5, islandLook);
    half luma = dot(col, half3(0.299, 0.587, 0.114));
    half3 rainGrey = lerp(half3(luma, luma, luma), _SlugAirColor.rgb, 0.35);
    col = lerp(col, rainGrey, farRain);

    float d = dist * _SlugAir.x;
    float most = _SlugAir.y > 0.0 ? _SlugAir.y : 1.0;
    float charKeep = lerp(1.0 - 0.32 * _SlugWet.z * smoothstep(8.0, 45.0, dist) * step(0.5, _SlugWet.w), 1.0, step(0.5, islandLook));
    col = lerp(col, _SlugAirColor.rgb, (1.0 - exp(-d * d)) * most * charKeep);
    return lerp(col, _SlugMistColor.rgb, SlugMistAmount(positionWS));
}

half3 SlugAir(half3 col, float3 positionWS)
{
    return SlugAirLook(col, positionWS, 1.0);
}

// the dawn / dusk light on high ground (lit: 0 shade .. 1 full light)
half3 SlugGlow(half3 albedo, float3 positionWS, float lit)
{
    if (_SlugGlowRange.x <= 0.0) return 0;
    float h = saturate((positionWS.y - _SlugGlow.w) / _SlugGlowRange.x);
    return albedo * _SlugGlow.rgb * (h * h) * saturate(lit * 1.6 - 0.3);
}

// the ground colour under a world point (rgb), and 1 where the island's ground map covers it
half4 SlugGroundAt(float3 positionWS)
{
    if (_SlugGroundRect.w < 0.5) return 0;
    float2 uv = (positionWS.xz - _SlugGroundRect.xy) * _SlugGroundRect.z;
    if (any(uv < 0.0) || any(uv > 1.0)) return 0;
    return half4(SAMPLE_TEXTURE2D_LOD(_SlugGround, sampler_SlugGround, uv, 0).rgb, 1);
}

// rocks wear the ground of their place on the faces that look up (salt dust on the salt mountain, moss in the forest,
// sand in the desert); a broken edge so it reads as a coat, not a cap
half3 SlugTopCoat(half3 albedo, float3 positionWS, float3 normalWS, float amount, float noise)
{
    if (amount <= 0.0) return albedo;
    half4 g = SlugGroundAt(positionWS);
    float up = smoothstep(0.45 + 0.25 * noise, 0.62 + 0.25 * noise, normalWS.y);
    return lerp(albedo, g.rgb, up * amount * g.a);
}

#endif
