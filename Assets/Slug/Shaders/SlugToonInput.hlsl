#ifndef SLUG_TOON_INPUT_INCLUDED
#define SLUG_TOON_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// ---- Material parameters (SRP Batcher compatible: shared by every pass) ----
CBUFFER_START(UnityPerMaterial)
    half4 _BaseColor;
    half4 _ShadeColor;
    half4 _BellyColor;
    half  _BellyAmount;

    half  _PatternType;      // 0 none / 1 spots / 2 rings / 3 stripes / 4 sparkle
    half4 _PatternColor;
    half4 _PatternScale;     // x: cells around, y: cells along
    half  _PatternSize;

    half  _Bands;
    half  _BandSoftness;
    half  _AmbientStrength;

    half4 _RimColor;
    half  _RimPower;
    half  _RimAmount;

    half4 _OutlineColor;
    half  _OutlineWidth;

    half  _BodyLength;
    half  _WaveAmp;
    half  _WaveFreq;
    half  _WaveSpeed;
    half  _WavePhase;
    half  _WaveHeadFade;

    half  _BumpAmp;
    half  _BumpFreq;
    half  _DentAmp;
    half  _DentFreq;

    half  _FeltAmount;
    half  _FuzzAmount;

    half4 _StitchColor;
    half  _StitchAmount;
    half  _StitchDash;
    half  _StitchWidth;
    half  _StitchRingT;

    half  _Facet;

    half  _Alpha;
    half  _AlphaEdge;
    half  _JiggleAmp;
    half  _JiggleFreq;
    half  _JiggleSpeed;

    half  _RuffleAmp;
    half  _RuffleFreq;
    half  _RuffleSpeed;

    half4 _EmissionColor;
    half  _SparkleStrength;

    half  _ToonSmooth;       // 0 = stepped toon bands, 1 = smooth gradient
    half  _WrinkleAmount;    // skin grooves / wrinkles
    half4 _WrinkleScale;     // x: cells around (integer), y: cells along
    half  _MottleAmount;     // blotchy natural mottling
    half4 _MottleColor;
    half  _MottleScale;
    half  _SkirtAmp;         // the foot fringe: a soft wave along the edge where the sole meets the flank
    half  _SkirtFreq;        // waves along the body (per unit t)
    half  _SkirtU;           // where the edge is: distance round the body from the middle of the sole (uv.x)
    half  _SkirtWidth;       // how wide the fringe band is (uv.x)
    half  _SkirtLines;       // how dark the pleat lines across it are (0 = none)
    half  _SkirtEnd;         // the fringe runs from the tail to here along the body (the part on the ground)
    half  _CamoAmount;       // camouflage for worn gear: hard-edged blotches of three colours over the base
    half4 _CamoColor1;
    half4 _CamoColor2;
    half4 _CamoColor3;
    half  _CamoScale;

    // face decals drawn on the body surface (so they follow the crawl wave exactly)
    float4 _EyeC;            // xyz: eye centre in object space (right eye, mirrored by |x|), w: enabled
    float4 _EyeR;            // xyz: outward axis (+x side)
    float4 _EyeU;            // xyz: up axis
    float4 _EyeShape;        // x: 0 dot 1 tall 2 wide 3 half-lid 4 dot+rim 5 line, y: a, z: b
    float4 _EyeP;            // x: lid cut (fraction of b), y: lid tilt, z: rim width / lid line width
    half4  _EyeColor;
    half4  _EyeRimColor;
    float4 _MouthC;
    float4 _MouthR;
    float4 _MouthU;
    float4 _MouthP;          // x: half width, y: depth, z: thickness, w: amount (0 = off)
    half4  _MouthColor;

    // 3D eyeball surface (iris / pupil drawn procedurally in the eye's local frame, see SlugEye.hlsl)
    half   _EyeKind;         // 0 = not an eye, 1 reptile 2 frog 3 octopus 4 fish 5 nictitating membrane 6 bead under lids
    float4 _EyeFC;           // eye centre (object space)
    float4 _EyeFX;           // local axes (object space): x outward, y up, z out of the surface
    float4 _EyeFY;
    float4 _EyeFZ;
    half   _EyeRad;
    half4  _EyeIris1;
    half4  _EyeIris2;
    half4  _EyeSclera;

    // whole-body crawl: the length breathes (stretch / shorten), the front swells when it shortens
    half   _Stretch;         // amplitude of the length change (0.1 = +-10%)
    float4 _TailPivot;       // pivot of the tail-end ring (the body stretches away from it)
    half   _RadiusMin;       // lower / upper limit of the radial scale: a thin part never turns inside out
    half   _RadiusMax;

    // flat face painted on the skin (object space, so it rides the wave)
    float4 _FaceC;           // xyz: face centre on the body midline, w: enabled
    float4 _FaceR;           // xyz: lateral axis (+x), w: unused
    float4 _FaceU;           // xyz: up axis
    float4 _FaceN;           // xyz: outward normal (hides the face on the back of the head)
    float4 _FaceEye;         // x: eye offset (sideways), y: eye height, z: eye radius, w: left eye shut (0..1)
    float4 _FaceMuz;         // x: muzzle centre height, y: half width, z: half height, w: edge softness
    float4 _FaceNos;         // x: nostril offset, y: height, z: radius x, w: radius y
    float4 _FaceMouth;       // x: mouth height, y: half length, z: half thickness, w: amount (0 = off)
    half4  _FaceMuzzleColor;
    half4  _FaceInkColor;

    // parts (ears, hands) that ride on the body wave as a whole
    half   _FollowMode;
    half   _FollowT;
    float4 _FollowNormal;
    float4 _FollowPivot;     // xyz: pivot of the ring the part grows from
    float4 _FollowRadial;    // xyz: attach point minus pivot, w: ring radius
    half   _SwayAmp;         // soft sway of the free end (tentacles)
    float4 _SwayDir;

    // keeping the lifted front upright while the whole slug is tilted (slopes, climbing)
    float  _UprightAngle;    // radians about the body x axis (+ = pitch the front forward / down)
    float4 _UprightPivot;    // xyz: pivot in body space
    float4 _UprightRange;    // x..y: the bend fades in along the body (uv.y)
    float4 _PartOffset;      // xyz: where this part sits in body space (zero for the body itself)

    // the rear trailing behind a turn: it swings about the body's turning axis, more toward the tail
    float  _TurnAngle;       // radians about the body y axis (the tail's lag behind the front)
    float4 _TurnPivot;       // xyz: a point on the turning axis, in body space
    float4 _TurnRange;       // x..y: the bend fades out along the body (uv.y): full at the tail, none from y on

    // lying down: the front sinks straight down (the neck squashes), more toward the head
    float  _DropAmount;      // how far the head sinks (body space)
    float4 _DropRange;       // x..y: the sinking fades in along the body (uv.y): none before x, all of it from y on

    // the head turned round on the neck (looking back at the bag)
    float  _TwistAngle;      // radians about the body y axis (+ = to the right)
    float4 _TwistPivot;      // xyz: a point on the neck's axis, in body space
    float4 _TwistRange;      // x..y: the turn fades in along the body (uv.y): none before x, all of it from y on

    // the rear following the path the slug crawled (up a wall, over an edge): it bends in the body's y-z plane
    float4 _PathAnchor;      // xyz: where the rear starts (body space, under the slug's feet), w: spacing of the knots (0 = off)
    float4 _PathA0;          // the rear's pitch (radians, + = rising behind) at knots 1..4 behind the anchor (0 at the anchor)
    float4 _PathA1;          // ... at knots 5..8; straight on from the last one

    // rocks on the island wear the ground of their place on the faces that look up (0 = off)
    half  _TopCoat;
    half  _WindAmount;
    half  _IslandLook;
CBUFFER_END

// Global overrides so screenshots can freeze the crawl animation at a known time.
float _SlugFreeze;
float _SlugTimeValue;

float SlugTime()
{
    return _SlugFreeze > 0.5 ? _SlugTimeValue : _Time.y;
}

// The air (haze, shade colour, valley mist, dawn glow, the ground map, wetness, cloud shadows) and wind
#include "SlugAir.hlsl"
#include "SlugWind.hlsl"

// 1 = island world object (rocks, plants, props: wets & shines up close, desaturates in far rain haze)
// 0 = slug character or worn gear (keeps its colour in the rain so bright bodies stand out afar)
float SlugIsIslandObject()
{
    if (_IslandLook < 0.5 || _BellyAmount > 0.001 || _WrinkleAmount > 0.001 || _FaceC.w > 0.5 || _EyeKind > 0.5 || _FollowMode > 0.5 || _WaveAmp > 0.001)
        return 0.0;
    return 1.0;
}

// ---- noise helpers ----
float Hash11(float p) { p = frac(p * 0.1031); p *= p + 33.33; p *= p + p; return frac(p); }
float Hash21(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}
float2 Hash22(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}
float Hash31(float3 p3)
{
    p3 = frac(p3 * 0.1031);
    p3 += dot(p3, p3.zyx + 31.32);
    return frac((p3.x + p3.y) * p3.z);
}
float ValueNoise3(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    float n000 = Hash31(i + float3(0, 0, 0));
    float n100 = Hash31(i + float3(1, 0, 0));
    float n010 = Hash31(i + float3(0, 1, 0));
    float n110 = Hash31(i + float3(1, 1, 0));
    float n001 = Hash31(i + float3(0, 0, 1));
    float n101 = Hash31(i + float3(1, 0, 1));
    float n011 = Hash31(i + float3(0, 1, 1));
    float n111 = Hash31(i + float3(1, 1, 1));
    return lerp(lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y),
                lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y), f.z);
}

// ---- vertex deformation: crawl wave / clay bumps / gummy jiggle / nudibranch ruffle ----
// uv.x: around the body (0 = back midline, 0.5 = sole), uv.y: along the body (0 = tail, 1 = head)
// Displacement of a surface point of the body at crawl time.
//   t     : position along the body (uv.y), 0 tail ... 1 head
//   pivot : sole-line point of the ring the point belongs to; radial: point - pivot; rho: ring radius
float3 BodyOffset(float t, float3 pivot, float3 radial, float3 nrm, float rho)
{
    float s = 1.0 + _Stretch * sin(6.2831853 * _WaveSpeed * SlugTime() + _WavePhase);
    // volume keeping: when the body gets shorter the FRONT swells; the tail never grows
    float G = 2.0 * smoothstep(0.35, 0.95, t);
    float k = clamp(1.0 + (rsqrt(s) - 1.0) * G, _RadiusMin, _RadiusMax);
    float3 off = (s - 1.0) * (pivot - _TailPivot.xyz) + radial * (k - 1.0);

    // peristaltic ripple tail -> head, fading toward the head, and limited by the local thickness
    float w = 1.0 - smoothstep(_WaveHeadFade, 1.0, t);
    float phase = 6.2831853 * (_WaveFreq * t - _WaveSpeed * SlugTime()) + _WavePhase;
    float d = sin(phase) * _WaveAmp * w;
    if (rho > 0.0) d = clamp(d, -0.4 * rho, 0.25 * rho);       // never inside out, never fatter than the root
    return off + nrm * d;
}

// Pitches the front of the body about _UprightPivot, more toward the head, so the lifted part stays upright
// when the whole slug is tilted. Mirrored on the CPU by SlugPop.Upright().
void UprightBend(inout float3 posOS, inout float3 nrmOS, float t)
{
    if (abs(_UprightAngle) < 1e-4) return;
    float a = _UprightAngle * smoothstep(_UprightRange.x, _UprightRange.y, t);
    float s = sin(a), c = cos(a);
    float3 p = posOS + _PartOffset.xyz - _UprightPivot.xyz;
    p = float3(p.x, p.y * c - p.z * s, p.y * s + p.z * c);
    posOS = p + _UprightPivot.xyz - _PartOffset.xyz;
    nrmOS = float3(nrmOS.x, nrmOS.y * c - nrmOS.z * s, nrmOS.y * s + nrmOS.z * c);
}

// Lying down: the front sinks straight down, more toward the head, so the neck squashes and the head stays upright.
// Mirrored on the CPU by SlugPop.Drop().
void ProneDrop(inout float3 posOS, float t)
{
    if (_DropAmount < 1e-5) return;
    posOS.y -= _DropAmount * smoothstep(_DropRange.x, _DropRange.y, t);
}

// Turns the head round on the neck (about the vertical axis through _TwistPivot), more toward the head.
// Mirrored on the CPU by SlugPop.Twist().
void HeadTwist(inout float3 posOS, inout float3 nrmOS, float t)
{
    if (abs(_TwistAngle) < 1e-4) return;
    float a = _TwistAngle * smoothstep(_TwistRange.x, _TwistRange.y, t);
    float s = sin(a), c = cos(a);
    float3 p = posOS + _PartOffset.xyz - _TwistPivot.xyz;
    p = float3(p.x * c + p.z * s, p.y, -p.x * s + p.z * c);
    posOS = p + _TwistPivot.xyz - _PartOffset.xyz;
    nrmOS = float3(nrmOS.x * c + nrmOS.z * s, nrmOS.y, -nrmOS.x * s + nrmOS.z * c);
}

// Swings the rear about the vertical turning axis, more toward the tail, so after a turn the tail trails behind and
// then follows (instead of the whole body turning rigidly). Mirrored on the CPU by SlugPop.Turn().
void TurnBend(inout float3 posOS, inout float3 nrmOS, float t)
{
    if (abs(_TurnAngle) < 1e-4) return;
    float a = _TurnAngle * (1.0 - smoothstep(_TurnRange.x, _TurnRange.y, t));
    float s = sin(a), c = cos(a);
    float3 p = posOS + _PartOffset.xyz - _TurnPivot.xyz;
    p = float3(p.x * c + p.z * s, p.y, -p.x * s + p.z * c);
    posOS = p + _TurnPivot.xyz - _PartOffset.xyz;
    nrmOS = float3(nrmOS.x * c + nrmOS.z * s, nrmOS.y, -nrmOS.x * s + nrmOS.z * c);
}

// Lays the rear (the rings behind the feet) along the path the slug crawled: the pitch goes linearly from knot to knot
// and every ring moves rigidly to its place on that curve, so the tail goes down a wall and over an edge after the
// front instead of sticking out. ringZ: body-space z of the ring the point belongs to. Mirrored on the CPU by SlugPop.Path().
void PathBend(inout float3 posOS, inout float3 nrmOS, float ringZ)
{
    float step = _PathAnchor.w;
    if (step <= 0.0) return;
    float d = _PathAnchor.z - ringZ;              // how far behind the anchor, along the body
    if (d <= 0.0) return;
    float a[9] = { 0.0, _PathA0.x, _PathA0.y, _PathA0.z, _PathA0.w, _PathA1.x, _PathA1.y, _PathA1.z, _PathA1.w };
    float2 s = float2(_PathAnchor.y, _PathAnchor.z);    // (y, z) on the curve
    float ang = a[8];
    float left = d;
    [unroll] for (int i = 0; i < 8; i++)
    {
        if (left > 0.0)
        {
            float L = min(left, step);
            float a0 = a[i], k = (a[i + 1] - a0) / step;
            float a1 = a0 + k * L;
            // the backward direction is (sin a, -cos a): integrate it over the piece
            if (abs(k) > 1e-3) s += float2(cos(a0) - cos(a1), sin(a0) - sin(a1)) / k;
            else { float am = 0.5 * (a0 + a1); s += float2(sin(am), -cos(am)) * L; }
            ang = a1;
            left -= L;
        }
    }
    if (left > 0.0) s += float2(sin(ang), -cos(ang)) * left;

    float sn = sin(ang), c = cos(ang);
    float3 p = posOS + _PartOffset.xyz;
    float2 o = float2(p.y - _PathAnchor.y, p.z - (_PathAnchor.z - d));    // off the straight rest line
    p.yz = s + float2(o.x * c - o.y * sn, o.x * sn + o.y * c);
    posOS = p - _PartOffset.xyz;
    nrmOS = float3(nrmOS.x, nrmOS.y * c - nrmOS.z * sn, nrmOS.y * sn + nrmOS.z * c);
}

// 0..1: how much a body point belongs to the foot fringe (near the sole's edge, on the part lying on the ground)
float SkirtBand(float2 uv)
{
    float du = abs(abs(uv.x - 0.5) - _SkirtU);
    float across = 1.0 - smoothstep(0.0, max(_SkirtWidth, 1e-3), du);
    float along = smoothstep(0.0, 0.08, uv.y) * (1.0 - smoothstep(_SkirtEnd - 0.12, _SkirtEnd, uv.y));
    return across * along;
}

// uv1.x: ring radius, uv2: ring pivot (both baked into the body mesh; zero on other meshes)
void SlugDeform(inout float3 posOS, inout float3 nrmOS, float3 tanOS, float2 uv, float rho, float3 pivot)
{
    // Ears / hands / eyeballs: move rigidly with the body surface point they grow from.
    if (_FollowMode > 0.5)
    {
        posOS += BodyOffset(_FollowT, _FollowPivot.xyz, _FollowRadial.xyz, _FollowNormal.xyz, _FollowRadial.w);
        if (_SwayAmp > 0.0)
        {
            float phs = 6.2831853 * (_WaveFreq * _FollowT - _WaveSpeed * SlugTime()) + _WavePhase;
            float h = uv.y;
            posOS += _SwayDir.xyz * (sin(phs + 1.3 * h) * _SwayAmp * h * h);
        }
        PathBend(posOS, nrmOS, _FollowPivot.z);
        ProneDrop(posOS, _FollowT);
        HeadTwist(posOS, nrmOS, _FollowT);
        UprightBend(posOS, nrmOS, _FollowT);
        TurnBend(posOS, nrmOS, _FollowT);
        return;
    }

    float3 basePos = posOS;
    float t = uv.y;

    float w = 1.0 - smoothstep(_WaveHeadFade, 1.0, t);
    float phase = 6.2831853 * (_WaveFreq * t - _WaveSpeed * SlugTime()) + _WavePhase;
    float amp = _WaveAmp * w;
    posOS += BodyOffset(t, pivot, basePos - pivot, nrmOS, rho);
    float slope = amp * 6.2831853 * _WaveFreq * cos(phase) / max(_BodyLength, 1e-3);
    nrmOS = normalize(nrmOS - tanOS * slope);

    // Hand-pressed clay lumps.
    if (_BumpAmp > 0.0)
    {
        float n = ValueNoise3(basePos * _BumpFreq) - 0.5;
        posOS += nrmOS * (n * 2.0 * _BumpAmp);
    }

    // Gummy wobble.
    if (_JiggleAmp > 0.0)
    {
        float j = sin(SlugTime() * _JiggleSpeed + dot(basePos, float3(_JiggleFreq, _JiggleFreq * 1.7, _JiggleFreq * 1.3)));
        posOS += nrmOS * (j * _JiggleAmp);
    }

    // Frilly mantle edge.
    if (_RuffleAmp > 0.0)
    {
        float side = pow(abs(sin(6.2831853 * uv.x)), 6.0);
        float r = sin(t * _RuffleFreq * 6.2831853 + SlugTime() * _RuffleSpeed);
        posOS += nrmOS * (r * _RuffleAmp * side);
    }

    // The foot fringe: the rim of the sole ripples softly, travelling with the crawl.
    if (_SkirtAmp > 0.0)
    {
        float band = SkirtBand(uv);
        float wave = 0.5 + 0.5 * sin(6.2831853 * (t * _SkirtFreq - _WaveSpeed * SlugTime()));
        posOS += nrmOS * (wave * _SkirtAmp * band);
    }

    PathBend(posOS, nrmOS, pivot.z);
    ProneDrop(posOS, t);
    HeadTwist(posOS, nrmOS, t);
    UprightBend(posOS, nrmOS, t);
    TurnBend(posOS, nrmOS, t);
}

#endif
