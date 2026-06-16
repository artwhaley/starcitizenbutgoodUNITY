using UnityEngine;

namespace FlightModel
{
    public enum GasPlumeProfile
    {
        Rcs,
        Engine
    }

    public static class ParticleVfxUtility
    {
        static Material glowMaterial;
        static Material rcsSmokeMaterial;
        static Material enginePlumeMaterial;

        public static Material GetGlowMaterial()
        {
            if (glowMaterial != null)
            {
                return glowMaterial;
            }

            Shader shader = Shader.Find("Particles/Standard Unlit")
                ?? Shader.Find("Mobile/Particles/Additive")
                ?? Shader.Find("Legacy Shaders/Particles/Additive")
                ?? Shader.Find("Sprites/Default");

            if (shader == null)
            {
                Debug.LogWarning("ParticleVfxUtility: no particle shader found; VFX may be invisible.");
                return null;
            }

            glowMaterial = new Material(shader);
            glowMaterial.name = "M_ParticleGlow_Runtime";
            glowMaterial.color = Color.white;

            if (glowMaterial.HasProperty("_Color"))
            {
                glowMaterial.SetColor("_Color", Color.white);
            }

            if (glowMaterial.HasProperty("_TintColor"))
            {
                glowMaterial.SetColor("_TintColor", new Color(1f, 1f, 1f, 0.85f));
            }

            Texture2D texture = CreateSoftGlowTexture();
            if (glowMaterial.HasProperty("_MainTex"))
            {
                glowMaterial.SetTexture("_MainTex", texture);
            }

            return glowMaterial;
        }

        public static void ApplyGlowMaterial(ParticleSystem particleSystem)
            => ApplyProfileMaterial(particleSystem, GasPlumeProfile.Engine);

        public static void ApplyProfileMaterial(ParticleSystem particleSystem, GasPlumeProfile profile)
        {
            if (particleSystem == null)
            {
                return;
            }

            Material material = profile == GasPlumeProfile.Rcs
                ? GetRcsSmokeMaterial()
                : GetEnginePlumeMaterial();

            material ??= GetGlowMaterial();
            if (material == null)
            {
                return;
            }

            ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.sharedMaterial = material;
            renderer.trailMaterial = material;
            renderer.sortingFudge = profile == GasPlumeProfile.Rcs ? 0.2f : 0.7f;

            if (profile == GasPlumeProfile.Engine)
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.lengthScale = 1.85f;
                renderer.velocityScale = 0.18f;
                renderer.cameraVelocityScale = 0f;
            }
            else
            {
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.lengthScale = 1.75f;
                renderer.velocityScale = 0.18f;
                renderer.cameraVelocityScale = 0f;
            }
        }

        public static void ApplyFallbackMaterial(ParticleSystem particleSystem, GasPlumeProfile profile)
        {
            if (particleSystem == null)
            {
                return;
            }

            ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            if (renderer == null || renderer.sharedMaterial != null)
            {
                return;
            }

            ApplyProfileMaterial(particleSystem, profile);
        }

        public static void ConfigureGasPlume(ParticleSystem particleSystem, GasPlumeProfile profile)
        {
            if (particleSystem == null)
            {
                return;
            }

            bool isRcs = profile == GasPlumeProfile.Rcs;

            var main = particleSystem.main;
            main.loop = !isRcs;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.emitterVelocityMode = ParticleSystemEmitterVelocityMode.Transform;
            main.startLifetime = isRcs
                ? new ParticleSystem.MinMaxCurve(0.14f, 0.28f)
                : new ParticleSystem.MinMaxCurve(0.2f, 0.46f);
            main.startSpeed = isRcs
                ? new ParticleSystem.MinMaxCurve(9f, 16f)
                : new ParticleSystem.MinMaxCurve(5.5f, 10.5f);
            main.startSize = isRcs
                ? new ParticleSystem.MinMaxCurve(0.07f, 0.16f)
                : new ParticleSystem.MinMaxCurve(0.34f, 0.82f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.maxParticles = isRcs ? 320 : 640;
            main.startColor = isRcs
                ? new Color(0.86f, 0.95f, 1f, 0.82f)
                : new Color(0.56f, 0.88f, 1f, 0.95f);

            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = isRcs ? 7f : 7f;
            shape.radius = isRcs ? 0.012f : 0.14f;
            shape.radiusThickness = 1f;

            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            if (isRcs)
            {
                gradient.SetKeys(
                    new[]
                    {
                        new GradientColorKey(new Color(0.95f, 1f, 1f), 0f),
                        new GradientColorKey(new Color(0.58f, 0.72f, 0.88f), 0.25f),
                        new GradientColorKey(new Color(0.26f, 0.29f, 0.34f), 1f)
                    },
                    new[]
                    {
                        new GradientAlphaKey(0.95f, 0f),
                        new GradientAlphaKey(0.5f, 0.36f),
                        new GradientAlphaKey(0f, 1f)
                    });
            }
            else
            {
                gradient.SetKeys(
                    new[]
                    {
                        new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(new Color(0.28f, 0.78f, 1f), 0.25f),
                        new GradientColorKey(new Color(0.18f, 0.35f, 1f), 0.68f),
                        new GradientColorKey(new Color(1f, 0.45f, 0.12f), 1f)
                    },
                    new[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0.76f, 0.45f),
                        new GradientAlphaKey(0f, 1f)
                    });
            }
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = isRcs
                ? new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, 0.32f),
                    new Keyframe(0.18f, 0.95f),
                    new Keyframe(1f, 0.42f)))
                : new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, 0.45f),
                    new Keyframe(0.16f, 1.25f),
                    new Keyframe(1f, 0.12f)));

            var velocityOverLifetime = particleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.speedModifier = isRcs
                ? new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, 1.25f),
                    new Keyframe(1f, 0.72f)))
                : new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, 0.98f),
                    new Keyframe(1f, 0.24f)));

            var limitVelocity = particleSystem.limitVelocityOverLifetime;
            limitVelocity.enabled = true;
            limitVelocity.dampen = isRcs ? 0.08f : 0.12f;
            limitVelocity.drag = isRcs ? 0.25f : 0.42f;
            limitVelocity.multiplyDragByParticleSize = false;
            limitVelocity.multiplyDragByParticleVelocity = true;
            limitVelocity.limit = isRcs ? 24f : 11f;

            var noise = particleSystem.noise;
            noise.enabled = true;
            noise.strength = isRcs
                ? new ParticleSystem.MinMaxCurve(0.04f, 0.14f)
                : new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
            noise.frequency = isRcs ? 0.75f : 0.3f;
            noise.scrollSpeed = isRcs ? 2.8f : 0.5f;
            noise.damping = true;

            var lights = particleSystem.lights;
            lights.enabled = false;

            var trails = particleSystem.trails;
            trails.enabled = true;
            if (isRcs)
            {
                trails.ratio = 0.9f;
                trails.lifetime = new ParticleSystem.MinMaxCurve(0.05f, 0.11f);
                trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, 0.55f),
                    new Keyframe(1f, 0.02f)));
                trails.colorOverTrail = gradient;
            }
            else
            {
                trails.ratio = 0.28f;
                trails.lifetime = new ParticleSystem.MinMaxCurve(0.05f, 0.13f);
                trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, 0.38f),
                    new Keyframe(1f, 0f)));
                trails.colorOverTrail = gradient;
            }

            var rotationOverLifetime = particleSystem.rotationOverLifetime;
            rotationOverLifetime.enabled = false;

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        static Material GetRcsSmokeMaterial()
        {
            if (rcsSmokeMaterial != null)
            {
                return rcsSmokeMaterial;
            }

            rcsSmokeMaterial = CreateRuntimeParticleMaterial(
                "M_RcsSmoke_Runtime",
                new Color(0.75f, 0.9f, 1f, 0.85f),
                CreateSoftSmokeTexture(),
                additive: false);
            return rcsSmokeMaterial;
        }

        static Material GetEnginePlumeMaterial()
        {
            if (enginePlumeMaterial != null)
            {
                return enginePlumeMaterial;
            }

            enginePlumeMaterial = CreateRuntimeParticleMaterial(
                "M_EnginePlume_Runtime",
                new Color(0.55f, 0.85f, 1f, 0.95f),
                CreatePlumeTexture(),
                additive: true);
            return enginePlumeMaterial;
        }

        static Material CreateRuntimeParticleMaterial(string name, Color tint, Texture2D texture, bool additive)
        {
            Shader shader = FindParticleShader(additive);

            if (shader == null)
            {
                return null;
            }

            Material material = new(shader)
            {
                name = name,
                color = tint
            };

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", tint);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", tint);
            }

            if (material.HasProperty("_TintColor"))
            {
                material.SetColor("_TintColor", tint);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            ConfigureBlendMode(material, additive);
            return material;
        }

        static Shader FindParticleShader(bool additive)
        {
            bool usingScriptableRenderPipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;

            string[] candidates;
            if (usingScriptableRenderPipeline)
            {
                candidates = additive
                    ? new[]
                    {
                        "Universal Render Pipeline/Particles/Unlit",
                        "Particles/Standard Unlit",
                        "Mobile/Particles/Additive",
                        "Legacy Shaders/Particles/Additive",
                        "Sprites/Default",
                        "Unlit/Transparent"
                    }
                    : new[]
                    {
                        "Universal Render Pipeline/Particles/Unlit",
                        "Particles/Standard Unlit",
                        "Mobile/Particles/Alpha Blended",
                        "Legacy Shaders/Particles/Alpha Blended",
                        "Sprites/Default",
                        "Unlit/Transparent"
                    };
            }
            else
            {
                candidates = additive
                    ? new[]
                {
                    "Mobile/Particles/Additive",
                    "Legacy Shaders/Particles/Additive",
                    "Particles/Standard Unlit",
                    "Unlit/Transparent",
                    "Sprites/Default"
                }
                    : new[]
                {
                    "Mobile/Particles/Alpha Blended",
                    "Legacy Shaders/Particles/Alpha Blended",
                    "Particles/Standard Unlit",
                    "Unlit/Transparent",
                    "Sprites/Default"
                };
            }

            for (int i = 0; i < candidates.Length; i++)
            {
                Shader shader = Shader.Find(candidates[i]);
                if (shader != null && shader.isSupported)
                {
                    return shader;
                }
            }

            return null;
        }

        static void ConfigureBlendMode(Material material, bool additive)
        {
            if (material == null)
            {
                return;
            }

            if (material.shader == null || !material.shader.isSupported)
            {
                Debug.LogWarning($"ParticleVfxUtility: refusing unsupported VFX shader on {material.name}.");
                return;
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", additive ? 1f : 2f);
            }

            if (material.HasProperty("_Blend"))
            {
                material.SetFloat("_Blend", additive ? 1f : 0f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", additive ? (float)UnityEngine.Rendering.BlendMode.SrcAlpha : (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            if (additive)
            {
                material.EnableKeyword("_BLENDMODE_ADD");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            else
            {
                material.DisableKeyword("_BLENDMODE_ADD");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }
        }

        static Texture2D CreateSoftGlowTexture()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "T_GlowSoft_Runtime",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            float center = (size - 1) * 0.5f;
            float radius = center;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / radius;
                    float dy = (y - center) / radius;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha *= alpha;
                    byte a = (byte)(alpha * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        static Texture2D CreateSoftSmokeTexture()
        {
            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "T_RcsSmoke_Runtime",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            float center = (size - 1) * 0.5f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - center) / center;
                    float ny = (y - center) / center;
                    float dist = Mathf.Sqrt(nx * nx + ny * ny);
                    float soft = Mathf.Clamp01(1f - dist);
                    float wisps = 0.72f + 0.28f * Mathf.Sin((nx * 17f) + (ny * 23f));
                    byte a = (byte)(soft * soft * wisps * 255f);
                    pixels[y * size + x] = new Color32(215, 235, 255, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        static Texture2D CreatePlumeTexture()
        {
            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "T_EnginePlume_Runtime",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            float center = (size - 1) * 0.5f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - center) / center;
                    float ny = (y - center) / center;
                    float dist = Mathf.Sqrt(nx * nx + ny * ny);
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha = Mathf.Pow(alpha, 2.8f);
                    Color core = Color.Lerp(new Color(0.1f, 0.45f, 1f, 1f), Color.white, alpha);
                    pixels[y * size + x] = new Color32(
                        (byte)(core.r * 255f),
                        (byte)(core.g * 255f),
                        (byte)(core.b * 255f),
                        (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
