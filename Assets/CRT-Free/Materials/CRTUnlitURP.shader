Shader "Hidden/CRTUnlitURP"
{
    Properties
    {
        _MainTex       ("Texture",       2D)    = "white" {}
        _BorderTex     ("BorderTexture", 2D)    = "white" {}
        _BorderTint    ("BorderTint",  Color)   = (1,1,1,1)

        _MaxColorsRed   ("MaxRedColors",   Range(0,256)) = 0
        _MaxColorsGreen ("MaxGreenColors", Range(0,256)) = 0
        _MaxColorsBlue  ("MaxBlueColors",  Range(0,256)) = 0

        _Curvature      ("Curvature",  Range(0,20))  = 0.2
        _Curvature2     ("Curvature2", Range(0,0.2)) = 0.05
        _VigSize        ("VigSize",    Range(0,1))   = 0.1
        _ColorScans     ("ColorScans", Vector)       = (0,0,0,0)

        _BorderZoom     ("BorderZoom", Range(0.5,2.5)) = 1
        _Desaturation   ("Desaturation", Range(0,1))  = 0

        _BorderOutterSizeX ("BorderOutterSizeX", Range(0,0.5)) = 0.2
        _BorderOutterSizeY ("BorderOutterSizeY", Range(0,0.5)) = 0.2
        _BorderOutterRound ("BorderOutterRound", Range(0,0.2)) = 0.01

        _BorderInnerSizeX ("BorderInnerSizeX", Range(0,0.5)) = 0.2
        _BorderInnerSizeY ("BorderInnerSizeY", Range(0,0.5)) = 0.2
        _BorderInnerDarkerAmount ("BorderInnerDarkerAmount", Range(0,1)) = 0.5

        _BorderInnerSharpness ("BorderInnerSharpness", Range(0,1)) = 0.2
        _BorderOutterSharpness("BorderOutterSharpness",Range(0,1)) = 0.2

        _CrtReflectionCurve   ("CrtReflectionCurve",  Range(0,10))   = 0.1
        _CrtReflectionRadius  ("CrtReflectionRadius", Range(-0.1,0.1)) = 0.05
        _CrtReflectionFalloff ("CrtReflectionFalloff",Range(0,1))    = 0
        _CrtGlowAmount        ("CrtGlowAmount",       Range(0,0.2))  = 0.1

        _Spread  ("DitherSpread4", Range(0,1)) = 0.5
        _Spread8 ("DitherSpread8", Range(0,1)) = 0.5
        _DitherScreenScale("DitherScreenScale", Range(0.5,2)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "CRT_URP"

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // ---- Tekstürler ----
            // URP Blit.hlsl defines _BlitTexture and sampler_LinearClamp already.
            TEXTURE2D(_BorderTex); SAMPLER(sampler_BorderTex);

            float4 _BorderTex_ST;
            float4 _BorderTint;

            // ---- CRT Parametreleri ----
            float _MaxColorsRed, _MaxColorsGreen, _MaxColorsBlue;
            float _Curvature, _Curvature2, _VigSize;
            float4 _ColorScans;
            float _BorderZoom, _Desaturation;
            float _BorderOutterSizeX, _BorderOutterSizeY;
            float _BorderInnerSizeX,  _BorderInnerSizeY;
            float _BorderOutterRound;
            float _BorderInnerSharpness, _BorderOutterSharpness;
            float _CrtReflectionCurve, _CrtReflectionRadius, _CrtReflectionFalloff, _CrtGlowAmount;
            float _BorderInnerDarkerAmount;
            float _Spread, _Spread8, _DitherScreenScale;

            // ---- Dithering ----
            uniform int _BrewedInk_Bayer4[16];
            uniform int _BrewedInk_Bayer8[64];

            // ---- Yardımcı Fonksiyonlar ----
            float roundBox(float2 p, float2 b, float r)
            {
                return length(max(abs(p) - b, 0.0)) - r;
            }

            float2 borderReflect(float2 p, float r)
            {
                float eps = 0.0001;
                float2 epsx = float2(eps, 0.0);
                float2 epsy = float2(0.0, eps);
                float2 b = (1.0 + float2(r, r)) * 0.5;
                r /= 3.0;
                p -= 0.5;
                float2 normal = float2(
                    roundBox(p - epsx, b, r) - roundBox(p + epsx, b, r),
                    roundBox(p - epsy, b, r) - roundBox(p + epsy, b, r)
                ) / eps;
                float d = roundBox(p, b, r);
                p += 0.5;
                return p + d * normal;
            }

            float2 CurvedSurface(float2 uv, float r)
            {
                return r * uv / sqrt(r * r - dot(uv, uv));
            }

            float2 crtCurve(float2 uv, float r)
            {
                r = 3.0 * r;
                uv = CurvedSurface(uv, r);
                return (uv / 2.0) + 0.5;
            }

            float4 sampleColor(float2 screenUv, float2 warpedUv)
            {
                int n4 = 4;
                int x4 = (int)(screenUv.x * _ScreenParams.x * _DitherScreenScale) % n4;
                int y4 = (int)(screenUv.y * _ScreenParams.y * _DitherScreenScale) % n4;
                float m4 = (_BrewedInk_Bayer4[y4 * n4 + x4] * 1.0 / (n4 * n4)) - 0.5;

                int n8 = 8;
                int x8 = (int)(screenUv.x * _ScreenParams.x * _DitherScreenScale) % n8;
                int y8 = (int)(screenUv.y * _ScreenParams.y * _DitherScreenScale) % n8;
                float m8 = (_BrewedInk_Bayer8[y8 * n8 + x8] * 1.0 / (n8 * n8)) - 0.5;

                // URP Blitter uses _BlitTexture and SAMPLE_TEXTURE2D_X
                float4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, warpedUv)
                             + m4 * _Spread + m8 * _Spread8;

                col.r = _MaxColorsRed   <= 0 ? col.r : floor(col.r * (_MaxColorsRed   - 1) + 0.5) / (_MaxColorsRed   - 1);
                col.g = _MaxColorsGreen <= 0 ? col.g : floor(col.g * (_MaxColorsGreen - 1) + 0.5) / (_MaxColorsGreen - 1);
                col.b = _MaxColorsBlue  <= 0 ? col.b : floor(col.b * (_MaxColorsBlue  - 1) + 0.5) / (_MaxColorsBlue  - 1);
                col.rgb = clamp(col.rgb, 0, 1);

                float grey = 0.21 * col.r + 0.71 * col.g + 0.07 * col.b;
                col.rgb = lerp(col.rgb, grey, _Desaturation);

                float t = _Time.z * _ColorScans.w;
                float s = (sin(_ScreenParams.y * screenUv.y * _ColorScans.z + t) + 1) * _ColorScans.x + 1;
                float c = (cos(_ScreenParams.y * screenUv.y * _ColorScans.z + t) + 1) * _ColorScans.y + 1;
                col.g  *= s;
                col.rb *= c;

                float2 absUv       = abs(warpedUv * 2.0 - 1.0);
                float2 invertAbsUv = 1.0 - absUv;
                float vigSize      = lerp(0, 500, _VigSize);
                float2 v           = float2(vigSize / _ScreenParams.x, vigSize / _ScreenParams.y);
                float2 vig         = smoothstep(float2(0,0), v, invertAbsUv);
                col *= vig.x * vig.y;
                col  = clamp(col, 0, 1);
                return col;
            }

            // ---- Fragment Shader ----
            float4 frag(Varyings IN) : SV_Target
            {
                // Blit.hlsl defines Varyings with 'texcoord' instead of 'uv'
                float2 uvInput = IN.texcoord;
                float2 p = uvInput * 2.0 - 1.0;
                p *= _BorderZoom;
                p += p * dot(p, p) * _Curvature2;

                float2 borderUv = p;
                float boundOut = roundBox(borderUv,
                    float2(1 + _BorderOutterSizeX, 1 + _BorderOutterSizeY),
                    _BorderOutterRound) * lerp(5, 100, _BorderOutterSharpness);
                boundOut = clamp(boundOut, 0.0, 1.0);

                float innerBorderScale = lerp(5, 100, _BorderInnerSharpness);
                float boundIn = roundBox(borderUv,
                    float2(1 - _BorderInnerSizeX, 1 - _BorderInnerSizeY),
                    _BorderOutterRound) * innerBorderScale;
                boundIn = clamp(boundIn, 0.0, 1.0);

                float insideMask  = boundIn - boundOut;
                float outsideMask = boundOut;

                float insideArg = 4.0 * (1.0 - roundBox(borderUv,
                    float2(1 - _BorderInnerSizeX, 1 - _BorderInnerSizeY),
                    _BorderOutterRound) * lerp(1, 70, _CrtReflectionFalloff));
                insideArg = clamp(insideArg, 0, 1);

                float4 borderColor = SAMPLE_TEXTURE2D(_BorderTex, sampler_BorderTex,
                    p * _BorderTex_ST.xy + _BorderTex_ST.zw);
                borderColor.rgb = lerp(borderColor.rgb * _BorderTint.rgb,
                                       _BorderTint.rgb, 1 - _BorderTint.a);

                float2 uv          = p * (1 - _BorderOutterRound);
                float2 offset      = uv / _Curvature;
                float2 curvedSpace = uv + uv * offset * offset;
                float2 mappedUv    = curvedSpace * 0.5 + 0.5;

                float2 crt  = crtCurve(curvedSpace, _CrtReflectionCurve);
                float2 qUv  = borderReflect(crt, _CrtReflectionRadius);

                float4 qColor = insideMask * insideArg * sampleColor(uvInput, qUv);
                float4 col    = sampleColor(uvInput, mappedUv);
                float screenMask = 1.0 - boundIn;

                return col * screenMask
                     + (_CrtGlowAmount * qColor + _BorderInnerDarkerAmount * borderColor * insideMask)
                     + (borderColor * outsideMask);
            }
            ENDHLSL
        }
    }
}
