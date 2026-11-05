using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NightShift
{
    internal sealed class AltitudeBackdrop
    {
        private readonly struct Atmosphere
        {
            internal readonly Color Sky;
            internal readonly Color Fog;
            internal readonly float Near;
            internal readonly float Far;

            internal Atmosphere(Color sky, Color fog, float near, float far)
            {
                Sky = sky;
                Fog = fog;
                Near = near;
                Far = far;
            }

            internal static Atmosphere Blend(Atmosphere a, Atmosphere b, float weight) =>
                new Atmosphere(Color.Lerp(a.Sky, b.Sky, weight), Color.Lerp(a.Fog, b.Fog, weight),
                    Mathf.Lerp(a.Near, b.Near, weight), Mathf.Lerp(a.Far, b.Far, weight));
        }

        private readonly struct HeightBand
        {
            internal readonly float From;
            internal readonly float To;
            internal readonly Atmosphere Look;

            internal HeightBand(float from, float to, Atmosphere look)
            {
                From = from;
                To = to;
                Look = look;
            }
        }

        private static readonly Atmosphere City = new Atmosphere(
            new Color(0.085f, 0.112f, 0.153f), new Color(0.084f, 0.116f, 0.156f), 30f, 92f);
        private static readonly HeightBand[] Bands =
        {
            new HeightBand(35f, 44f, new Atmosphere(
                new Color(0.20f, 0.26f, 0.33f), new Color(0.26f, 0.33f, 0.41f), 25f, 57f)),
            new HeightBand(45f, 58f, new Atmosphere(
                new Color(0.43f, 0.51f, 0.59f), new Color(0.45f, 0.52f, 0.59f), 32f, 79f))
        };

        private readonly Transform city;
        private readonly Transform cloudRoot;
        private readonly List<Transform> clouds = new List<Transform>();
        private readonly Material cloudMaterial;
        private readonly Texture2D cloudTexture;

        internal AltitudeBackdrop(Transform parent, Transform city, Shader shader)
        {
            this.city = city;
            cloudRoot = new GameObject("World-space low cloud band").transform;
            cloudRoot.SetParent(parent, false);
            shader = shader != null ? shader : Shader.Find("Unlit/Transparent");
            if (shader == null)
                return;
            cloudTexture = CreateCloudTexture();
            cloudMaterial = new Material(shader) { name = "Procedural cloud softness", mainTexture = cloudTexture };
            var random = new System.Random(8761);
            for (int i = 0; i < 17; i++)
            {
                float angle = (i / 17f) * Mathf.PI * 2f;
                float radius = 12f + (float)random.NextDouble() * 25f;
                var cloud = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cloud.name = "Cloud bank " + (i + 1);
                cloud.transform.SetParent(cloudRoot, false);
                cloud.transform.position = new Vector3(Mathf.Cos(angle) * radius,
                    31f + (float)random.NextDouble() * 8f, Mathf.Sin(angle) * radius);
                cloud.transform.localScale = new Vector3(13f + (float)random.NextDouble() * 15f,
                    3.6f + (float)random.NextDouble() * 3.5f, 1f);
                var renderer = cloud.GetComponent<Renderer>();
                renderer.sharedMaterial = cloudMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                Object.Destroy(cloud.GetComponent<Collider>());
                clouds.Add(cloud.transform);
            }
        }

        internal void Apply(Camera camera)
        {
            float height = camera.transform.position.y;
            Atmosphere look = City;
            foreach (HeightBand band in Bands)
            {
                if (height <= band.From)
                    break;
                float weight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(band.From, band.To, height));
                look = Atmosphere.Blend(look, band.Look, weight);
            }

            camera.backgroundColor = look.Sky;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = look.Fog;
            RenderSettings.fogStartDistance = look.Near;
            RenderSettings.fogEndDistance = look.Far;
            bool showClouds = height >= 35f;
            cloudRoot.gameObject.SetActive(showClouds);
            if (showClouds)
            {
                foreach (Transform cloud in clouds)
                {
                    Vector3 forward = cloud.position - camera.transform.position;
                    if (forward.sqrMagnitude > 0.01f)
                        cloud.rotation = Quaternion.LookRotation(forward);
                }
            }
            if (city != null)
                city.gameObject.SetActive(height < 66f);
        }

        internal void Dispose()
        {
            Object.Destroy(cloudMaterial);
            Object.Destroy(cloudTexture);
        }

        private static Texture2D CreateCloudTexture()
        {
            const int size = 96;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "Generated cloud puff",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = 2f * x / (size - 1) - 1f;
                float v = 2f * y / (size - 1) - 1f;
                float noise = Mathf.PerlinNoise(u * 3.2f + 7f, v * 4.3f + 9f);
                float round = Mathf.Clamp01(1f - (u * u + v * v) * 1.18f);
                float alpha = Mathf.Pow(round, 1.35f) * Mathf.Lerp(0.28f, 0.66f, noise);
                Color color = Color.Lerp(new Color(0.52f, 0.61f, 0.70f), new Color(0.74f, 0.79f, 0.83f),
                    Mathf.Clamp01(0.43f + v * 0.35f + noise * 0.17f));
                color.a = alpha;
                pixels[y * size + x] = color;
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }
    }
}
