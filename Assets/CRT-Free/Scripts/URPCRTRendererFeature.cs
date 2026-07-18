using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace BrewedInk.CRT
{
    /// <summary>
    /// URP 17 / Unity 6 RenderGraph uyumlu CRT post-process Renderer Feature.
    /// Kamera üzerindeki CRTCameraBehaviour bileşeninden değerleri dinamik olarak okur.
    /// </summary>
    public class URPCRTRendererFeature : ScriptableRendererFeature
    {
        [Header("CRT Material")]
        [Tooltip("Assets/CRT-Free/Materials/CRTMaterialURP.mat dosyasını atayın.")]
        public Material crtMaterial;

        [Header("Uygulama zamanı")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

        CRTBlitPass _pass;

        public override void Create()
        {
            _pass = new CRTBlitPass { renderPassEvent = renderPassEvent };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (crtMaterial == null) return;
            // Hem Game hem de Scene View kameraları için çalıştır
            var cameraType = renderingData.cameraData.cameraType;
            if (cameraType != CameraType.Game && cameraType != CameraType.SceneView) return;

            _pass.crtMaterial = crtMaterial;
            renderer.EnqueuePass(_pass);
        }

        // ---------------------------------------------------------------- pass

        class CRTBlitPass : ScriptableRenderPass
        {
            public Material crtMaterial;

            const string PassName     = "CRT_Effect";
            const string PassNameCopy = "CRT_CopyBack";

            static readonly float[] Bayer4 =
            {
                 0,  8,  2, 10,
                12,  4, 14,  6,
                 3, 11,  1,  9,
                15,  7, 13,  5
            };
            static readonly float[] Bayer8 =
            {
                 0, 32,  8, 40,  2, 34, 10, 42,
                48, 16, 56, 24, 50, 18, 58, 26,
                12, 44,  4, 36, 14, 46,  6, 38,
                60, 28, 52, 20, 62, 30, 54, 22,
                 3, 35, 11, 43,  1, 33,  9, 41,
                51, 19, 59, 27, 49, 17, 57, 25,
                15, 47,  7, 39, 13, 45,  5, 37,
                63, 31, 55, 23, 61, 29, 53, 21
            };

            class BlitPassData
            {
                public TextureHandle src;
                public Material      mat;
                public float[]       bayer4;
                public float[]       bayer8;
            }

            class CopyPassData
            {
                public TextureHandle src;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (crtMaterial == null) return;

                var resourceData = frameData.Get<UniversalResourceData>();
                var cameraData   = frameData.Get<UniversalCameraData>();

                TextureHandle activeColor = resourceData.activeColorTexture;
                if (!activeColor.IsValid()) return;

                // Kamera üzerindeki CRTCameraBehaviour bileşenini bul ve değerlerini materyale uygula
                var camera = cameraData.camera;
                if (camera != null)
                {
                    var behaviour = camera.GetComponent<CRTCameraBehaviour>();
                    if (behaviour != null && behaviour.data != null)
                    {
                        UpdateMaterialProperties(crtMaterial, behaviour.data);
                    }
                }

                var descriptor = cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0;
                descriptor.msaaSamples     = 1;

                TextureHandle temp = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph, descriptor, "_CRT_Temp", false);

                // ---- Pass 1: activeColor → temp (CRT shader) ----
                using (var builder = renderGraph.AddRasterRenderPass<BlitPassData>(
                           PassName, out var passData))
                {
                    passData.src    = activeColor;
                    passData.mat    = crtMaterial;
                    passData.bayer4 = Bayer4;
                    passData.bayer8 = Bayer8;

                    builder.UseTexture(activeColor, AccessFlags.Read);
                    builder.SetRenderAttachment(temp, 0, AccessFlags.Write);

                    builder.SetRenderFunc((BlitPassData data, RasterGraphContext ctx) =>
                    {
                        Shader.SetGlobalFloatArray("_BrewedInk_Bayer4", data.bayer4);
                        Shader.SetGlobalFloatArray("_BrewedInk_Bayer8", data.bayer8);
                        Blitter.BlitTexture(ctx.cmd, data.src,
                            new Vector4(1f, 1f, 0f, 0f), data.mat, 0);
                    });
                }

                // ---- Pass 2: temp → activeColor (geri yaz) ----
                using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>(
                           PassNameCopy, out var passData))
                {
                    passData.src = temp;

                    builder.UseTexture(temp, AccessFlags.Read);
                    builder.SetRenderAttachment(activeColor, 0, AccessFlags.Write);

                    builder.SetRenderFunc((CopyPassData data, RasterGraphContext ctx) =>
                    {
                        Blitter.BlitTexture(ctx.cmd, data.src,
                            new Vector4(1f, 1f, 0f, 0f), 0, false);
                    });
                }
            }

            private void UpdateMaterialProperties(Material mat, CRTData d)
            {
                // Renk kanalları
                mat.SetFloat("_MaxColorsRed",   d.maxColorChannels.red);
                mat.SetFloat("_MaxColorsGreen", d.maxColorChannels.green);
                mat.SetFloat("_MaxColorsBlue",  d.maxColorChannels.blue);
                mat.SetFloat("_Desaturation",   d.maxColorChannels.greyScale);

                // Dithering
                mat.SetFloat("_Spread",  d.dithering4);
                mat.SetFloat("_Spread8", d.dithering8);

                // Vignette / eğri
                mat.SetFloat("_VigSize",    d.vignette);
                mat.SetFloat("_Curvature",  d.innerCurve);
                mat.SetFloat("_Curvature2", d.monitorCurve);
                mat.SetFloat("_BorderZoom", d.zoom);

                // Dış çerçeve
                mat.SetFloat("_BorderOutterSizeX", d.monitorOutterSize.width);
                mat.SetFloat("_BorderOutterSizeY", d.monitorOutterSize.height);
                mat.SetFloat("_BorderOutterRound", d.monitorRoundness);

                // İç çerçeve
                mat.SetFloat("_BorderInnerSizeX",        d.monitorInnerSize.width);
                mat.SetFloat("_BorderInnerSizeY",        d.monitorInnerSize.height);
                mat.SetFloat("_BorderInnerDarkerAmount", 1f - d.innerMonitorDarkness);
                mat.SetFloat("_CrtGlowAmount",           d.innerMonitorShine);
                mat.SetFloat("_CrtReflectionRadius",     d.innerMonitorShineRadius);
                mat.SetFloat("_CrtReflectionCurve",      d.innerMonitorShineCurve);

                // Tarama çizgileri
                mat.SetVector("_ColorScans", new Vector4(
                    d.colorScans.greenChannelMultiplier,
                    d.colorScans.redBlueChannelMultiplier,
                    d.colorScans.sizeMultiplier,
                    2.0f // size multiplier 
                ));

                // Monitör rengi ve tekstürü
                mat.SetColor("_BorderTint", d.monitorColor);
                if (d.monitorTexture != null)
                    mat.SetTexture("_BorderTex", d.monitorTexture);
            }
        }
    }
}
