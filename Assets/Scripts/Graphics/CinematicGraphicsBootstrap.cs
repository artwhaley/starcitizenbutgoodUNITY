using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FlightModel
{
    public sealed class CinematicGraphicsBootstrap : MonoBehaviour
    {
        const float CameraRefreshInterval = 0.5f;

        static bool installed;
        float nextCameraRefresh;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (installed)
            {
                return;
            }

            installed = true;
            GameObject instance = new("Runtime Cinematic Graphics");
            DontDestroyOnLoad(instance);
            instance.AddComponent<CinematicGraphicsBootstrap>().ConfigureVolume();
        }

        void Update()
        {
            if (Time.unscaledTime < nextCameraRefresh)
            {
                return;
            }

            nextCameraRefresh = Time.unscaledTime + CameraRefreshInterval;
            ConfigureCameras();
        }

        void ConfigureVolume()
        {
            Volume volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            volume.weight = 1f;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Runtime Cinematic Flight Volume";
            volume.sharedProfile = profile;

            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.ACES;

            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.postExposure.overrideState = true;
            color.postExposure.value = 0.12f;
            color.contrast.overrideState = true;
            color.contrast.value = 18f;
            color.saturation.overrideState = true;
            color.saturation.value = 8f;

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.72f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.95f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.72f;
            bloom.clamp.overrideState = true;
            bloom.clamp.value = 65472f;
            bloom.highQualityFiltering.overrideState = true;
            bloom.highQualityFiltering.value = true;

            MotionBlur motionBlur = profile.Add<MotionBlur>(true);
            motionBlur.intensity.overrideState = true;
            motionBlur.intensity.value = 0.18f;
            motionBlur.clamp.overrideState = true;
            motionBlur.clamp.value = 0.045f;
            motionBlur.quality.overrideState = true;
            motionBlur.quality.value = MotionBlurQuality.High;

            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.18f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.44f;

            ChromaticAberration chromatic = profile.Add<ChromaticAberration>(true);
            chromatic.intensity.overrideState = true;
            chromatic.intensity.value = 0.035f;

            FilmGrain grain = profile.Add<FilmGrain>(true);
            grain.type.overrideState = true;
            grain.type.value = FilmGrainLookup.Thin1;
            grain.intensity.overrideState = true;
            grain.intensity.value = 0.055f;
            grain.response.overrideState = true;
            grain.response.value = 0.75f;

            LensDistortion lensDistortion = profile.Add<LensDistortion>(true);
            lensDistortion.intensity.overrideState = true;
            lensDistortion.intensity.value = -0.025f;

            ConfigureCameras();
        }

        void ConfigureCameras()
        {
            Camera[] cameras = Camera.allCameras;
            for (int i = 0; i < cameras.Length; i++)
            {
                ConfigureCamera(cameras[i]);
            }
        }

        static void ConfigureCamera(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            camera.allowHDR = true;
            camera.allowMSAA = true;

            UniversalAdditionalCameraData cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null)
            {
                cameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }

            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraData.antialiasingQuality = AntialiasingQuality.High;
            cameraData.dithering = true;
            cameraData.stopNaN = true;
        }
    }
}
