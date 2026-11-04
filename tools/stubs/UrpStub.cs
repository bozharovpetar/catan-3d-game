// Minimal URP / Core RP API surface for tools/compile-check.sh. NOT shipped in the game.
// Runtime/Editor code inside #if HEXHAVEN_URP may only use the members declared here.
using UnityEngine;

namespace UnityEngine.Rendering
{
    public class VolumeParameter<T> { public T value; public bool overrideState; public void Override(T x) { value = x; overrideState = true; } }
    public class FloatParameter : VolumeParameter<float> { public FloatParameter(float v, bool o = false) { value = v; } }
    public class MinFloatParameter : VolumeParameter<float> { public MinFloatParameter(float v, float min, bool o = false) { value = v; } }
    public class ClampedFloatParameter : VolumeParameter<float> { public ClampedFloatParameter(float v, float min, float max, bool o = false) { value = v; } }
    public class ColorParameter : VolumeParameter<Color> { public ColorParameter(Color v, bool o = false) { value = v; } }
    public class BoolParameter : VolumeParameter<bool> { public BoolParameter(bool v, bool o = false) { value = v; } }

    public class VolumeComponent : ScriptableObject { public bool active = true; }

    public class VolumeProfile : ScriptableObject
    {
        public T Add<T>(bool overrides = false) where T : VolumeComponent { return null; }
        public bool TryGet<T>(out T component) where T : VolumeComponent { component = null; return false; }
    }

    public class Volume : MonoBehaviour
    {
        public bool isGlobal = true;
        public float priority;
        public float weight = 1f;
        public VolumeProfile sharedProfile;
        public VolumeProfile profile;
    }
}

namespace UnityEngine.Rendering.Universal
{
    using UnityEngine.Rendering;

    public enum TonemappingMode { None, Neutral, ACES }
    public class TonemappingModeParameter : VolumeParameter<TonemappingMode> { public TonemappingModeParameter(TonemappingMode v, bool o = false) { value = v; } }

    public sealed class Bloom : VolumeComponent { public MinFloatParameter threshold = new MinFloatParameter(0.9f, 0f), intensity = new MinFloatParameter(0f, 0f); public ClampedFloatParameter scatter = new ClampedFloatParameter(0.7f, 0f, 1f); }
    public sealed class Tonemapping : VolumeComponent { public TonemappingModeParameter mode = new TonemappingModeParameter(TonemappingMode.None); }
    public sealed class ColorAdjustments : VolumeComponent { public FloatParameter postExposure = new FloatParameter(0f); public ClampedFloatParameter contrast = new ClampedFloatParameter(0f, -100f, 100f), saturation = new ClampedFloatParameter(0f, -100f, 100f); }
    public sealed class Vignette : VolumeComponent { public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f), smoothness = new ClampedFloatParameter(0.2f, 0.01f, 1f); public ColorParameter color = new ColorParameter(Color.black); }

    public enum AntialiasingMode { None, FastApproximateAntialiasing, SubpixelMorphologicalAntiAliasing, TemporalAntiAliasing }

    public class UniversalAdditionalCameraData : MonoBehaviour
    {
        public bool renderPostProcessing;
        public AntialiasingMode antialiasing;
        public bool renderShadows = true;
    }

    public class UniversalRenderPipelineAsset : RenderPipelineAsset
    {
        public float shadowDistance { get; set; }
        public int msaaSampleCount { get; set; }
        public bool supportsHDR { get; set; }
        public float renderScale { get; set; }
        public override RenderPipeline CreatePipeline() { return null; }
    }
}
