using UnityEngine;
using UnityEngine.Rendering;
#if ARSENAL_URP
using UnityEngine.Rendering.Universal;
#endif

namespace Arsenal.Game.Rendering
{
    /// <summary>Post-traitement cinématographique léger (URP) adapté au mobile.</summary>
    public static class PostProcessingSetup
    {
        public static void Apply(Camera camera, Transform parent, int quality)
        {
#if ARSENAL_URP
            if (GraphicsSettings.currentRenderPipeline == null) return;

            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = quality >= 1 ? AntialiasingMode.SubpixelMorphologicalAntiAliasing : AntialiasingMode.FastApproximateAntialiasing;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tonemap = profile.Add<Tonemapping>(true);
            tonemap.mode.Override(TonemappingMode.ACES);

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(quality >= 1 ? 0.55f : 0.3f);
            bloom.threshold.Override(1.05f);
            bloom.scatter.Override(0.6f);

            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.15f);
            color.contrast.Override(14f);
            color.saturation.Override(-6f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.32f);
            vignette.smoothness.Override(0.45f);

            var go = new GameObject("PostProcessVolume");
            go.transform.SetParent(parent, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
#endif
        }
    }
}
