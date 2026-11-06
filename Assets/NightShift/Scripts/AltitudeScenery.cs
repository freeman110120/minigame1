using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Batch = NightShift.NightShiftArt.MeshBatch;

namespace NightShift
{
    internal sealed class AltitudeScenery
    {
        internal const float HeavenHeight = 68f;
        internal const float OrbitHeight = 104f;

        private readonly List<Object> resources = new List<Object>();
        private readonly Transform heaven;
        private readonly Transform orbit;
        private readonly Transform birds;
        private readonly Transform plane;
        private readonly Transform saucer;
        private readonly Transform[] wings = new Transform[8];
        private readonly Material gold;
        private readonly Material amber;
        private readonly Material ivory;
        private readonly Material mist;
        private readonly Material steel;
        private readonly Material dark;
        private readonly Material cyan;
        private readonly Material green;
        private readonly Shader shader;
        private Transform flight;
        private Vector3 start;
        private Vector3 end;
        private float flightStart;
        private float duration;
        private float nextFlight;
        private int zone;

        internal AltitudeScenery(Transform parent, Shader fallback)
        {
            shader = Shader.Find("Unlit/Color") ?? fallback;
            gold = LitPaint("Brushed temple gold", new Color(.86f, .56f, .19f), .53f);
            amber = Paint("Bright gilt trim", 0xFFE0A0);
            ivory = LitPaint("Ivory stone", new Color(.85f, .76f, .61f), .13f);
            mist = LitPaint("Heaven cloud shadow", new Color(.72f, .72f, .84f), 0f);
            steel = Paint("Aircraft alloy", 0x9AB4CA);
            dark = Paint("Deep blue silhouette", 0x25334A);
            cyan = Paint("Ion blue", 0x92E6F4);
            green = Paint("Alien skin", 0xB7DD91);
            heaven = Group("Golden cloud realm", parent);
            orbit = Group("Orbital scenery", parent);
            birds = Group("Passing flock", parent);
            plane = Group("Passing airliner", parent);
            saucer = Group("Passing alien saucer", parent);
            BuildHeaven();
            BuildOrbit();
            BuildFlights();
            heaven.gameObject.SetActive(false);
            orbit.gameObject.SetActive(false);
            birds.gameObject.SetActive(false);
            plane.gameObject.SetActive(false);
            saucer.gameObject.SetActive(false);
        }

        internal void Apply(Camera camera)
        {
            float height = camera.transform.position.y;
            heaven.gameObject.SetActive(height >= HeavenHeight - 4f && height < OrbitHeight + 5f);
            orbit.gameObject.SetActive(height >= OrbitHeight - 3f);
            int newZone = height >= OrbitHeight ? 2 : height >= 35f && height < HeavenHeight ? 1 : 0;
            float now = Time.time;
            if (newZone != zone)
            {
                StopFlight();
                zone = newZone;
                nextFlight = now + Random.Range(1f, 2.6f);
            }
            if (zone == 0)
                return;
            if (flight == null && now >= nextFlight)
                Launch(camera, now);
            if (flight == null)
                return;

            float t = (now - flightStart) / duration;
            if (t >= 1f)
            {
                StopFlight();
                nextFlight = now + Random.Range(2.5f, 6.5f);
                return;
            }
            flight.position = Vector3.Lerp(start, end, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * .9f);
            if (flight == birds)
            {
                for (int i = 0; i < wings.Length; i++)
                {
                    float flap = Mathf.Sin(now * 13f + i / 2 * 1.4f) * 28f;
                    wings[i].localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? flap : -flap);
                }
            }
        }

        private void Launch(Camera camera, float now)
        {
            flight = zone == 2 ? saucer : Random.value < .62f ? birds : plane;
            bool rightward = Random.value < .5f;
            float y = zone == 2 ? Random.Range(.68f, .83f) : Random.Range(.48f, .79f);
            float depth = Random.Range(46f, 59f);
            start = camera.ViewportToWorldPoint(new Vector3(rightward ? -.23f : 1.23f, y, depth));
            end = camera.ViewportToWorldPoint(new Vector3(rightward ? 1.23f : -.23f, y + Random.Range(-.13f, .12f), depth));
            Vector3 heading = flight == saucer ? camera.transform.position - camera.ViewportToWorldPoint(
                new Vector3(.5f, y, depth)) : end - start;
            flight.SetPositionAndRotation(start, Quaternion.LookRotation(heading));
            flightStart = now;
            duration = Random.Range(6.5f, 10f);
            flight.gameObject.SetActive(true);
        }

        private void StopFlight()
        {
            if (flight != null)
                flight.gameObject.SetActive(false);
            flight = null;
        }

        private void BuildHeaven()
        {
            // Islands sit at fixed world positions so the tower climbs past them.
            Temple(new Vector3(-12f, 49f, 18f), .78f);
            Temple(new Vector3(8f, 59f, 27f), .64f);
            Temple(new Vector3(-13f, 73f, 23f), .83f);
        }

        private void Temple(Vector3 position, float scale)
        {
            Transform island = Group("Floating golden temple", heaven);
            island.localPosition = position;
            island.localScale = Vector3.one * scale;
            island.localRotation = Quaternion.Euler(0f, -18f, 0f);
            var mesh = new Batch();
            for (int i = 0; i < 7; i++)
            {
                float angle = i * Mathf.PI * 2f / 7f;
                mesh.Ellipsoid(new Vector3(Mathf.Cos(angle) * 5.2f, -.5f, Mathf.Sin(angle) * 3.6f),
                    new Vector3(3.35f, 1.6f + i % 3 * .24f, 2.6f), i % 2 == 0 ? mist : ivory);
            }
            for (int step = 0; step < 4; step++)
                mesh.Box(new Vector3(0f, step * .22f, 0f),
                    new Vector3(12f - step * .82f, .25f, 7.1f - step * .58f), step % 2 == 0 ? gold : ivory);
            for (int side = -1; side <= 1; side += 2)
            for (int column = -2; column <= 2; column++)
            {
                float x = column * 1.96f;
                float z = side * 2.23f;
                mesh.Box(new Vector3(x, .91f, z), new Vector3(.73f, .23f, .73f), amber);
                mesh.Box(new Vector3(x, 2.53f, z), new Vector3(.43f, 3.05f, .43f), ivory);
                mesh.Box(new Vector3(x, 1.24f, z), new Vector3(.58f, .13f, .58f), gold);
                mesh.Box(new Vector3(x, 3.88f, z), new Vector3(.58f, .13f, .58f), amber);
                mesh.Box(new Vector3(x, 4.08f, z), new Vector3(.76f, .28f, .76f), gold);
            }
            mesh.Box(new Vector3(0f, 4.37f, 0f), new Vector3(10.35f, .46f, 5.55f), amber);
            for (int tier = 0; tier < 5; tier++)
                mesh.Box(new Vector3(0f, 4.72f + tier * .25f, 0f),
                    new Vector3(10f - tier * .35f, .28f, 5.25f - tier * .96f), tier == 4 ? amber : gold);
            mesh.Ellipsoid(new Vector3(0f, 6f, 0f), new Vector3(1.3f, .69f, 1.25f), gold);
            mesh.Beam(new Vector3(0f, 6.45f, 0f), new Vector3(0f, 7.5f, 0f), .12f, amber);
            mesh.Ellipsoid(new Vector3(0f, 7.52f, 0f), Vector3.one * .19f, amber);
            Model("Colonnade and cloud island", island, mesh);
        }

        private void BuildOrbit()
        {
            var distant = new Batch();
            var random = new System.Random(1572);
            for (int i = 0; i < 440; i++)
            {
                var point = new Vector3(-50f + (float)random.NextDouble() * 80f,
                    30f + (float)random.NextDouble() * 170f, 26f + (float)random.NextDouble() * 32f);
                float size = .05f + (float)random.NextDouble() * .10f;
                distant.Box(point, Vector3.one * size, i % 7 == 0 ? amber : steel);
            }
            Vector3 planet = new Vector3(-27f, 69f, 34f);
            distant.Ellipsoid(planet, Vector3.one * 8.6f, PlanetPaint(), 48, 24);
            for (int i = 0; i < 48; i++)
            {
                Vector3 a = RingPoint(planet, i);
                Vector3 b = RingPoint(planet, i + 1);
                distant.Beam(a, b, .43f, i % 3 == 0 ? steel : ivory);
            }
            Model("Star field and ringed planet", orbit, distant);
        }

        private static Vector3 RingPoint(Vector3 center, int segment)
        {
            float angle = segment * Mathf.PI * 2f / 48f;
            return center + new Vector3(Mathf.Cos(angle) * 12.5f, Mathf.Sin(angle) * 3.7f, Mathf.Sin(angle) * 12f);
        }

        private void BuildFlights()
        {
            for (int i = 0; i < 4; i++)
            {
                var bird = Group("Bird", birds);
                bird.localPosition = new Vector3((i % 2 == 0 ? 1f : -1f) * i * .83f, i * .16f, -i * 1.05f);
                var body = new Batch();
                body.Ellipsoid(Vector3.zero, new Vector3(.14f, .11f, .32f), dark);
                Model("Bird body", bird, body);
                for (int side = 0; side < 2; side++)
                {
                    int sign = side == 0 ? 1 : -1;
                    var wing = Group("Flapping wing", bird);
                    wings[i * 2 + side] = wing;
                    var shape = new Batch();
                    shape.Beam(Vector3.zero, new Vector3(sign * .70f, .04f, -.14f), .106f, dark);
                    shape.Beam(new Vector3(sign * .65f, .04f, -.12f),
                        new Vector3(sign * 1.03f, .01f, -.32f), .071f, dark);
                    Model("Wing profile", wing, shape);
                }
            }
            var aircraft = new Batch();
            aircraft.Ellipsoid(Vector3.zero, new Vector3(.44f, .45f, 3.2f), ivory);
            aircraft.Ellipsoid(new Vector3(0f, .18f, 2.42f), new Vector3(.32f, .23f, .37f), dark);
            for (int side = -1; side <= 1; side += 2)
            {
                aircraft.Beam(Vector3.zero, new Vector3(side * 3.1f, .07f, -1.19f), .29f, steel);
                aircraft.Beam(new Vector3(0f, .23f, -2.1f), new Vector3(side * 1.16f, .29f, -2.76f), .20f, steel);
                aircraft.Ellipsoid(new Vector3(side * 1.12f, -.29f, -.2f), new Vector3(.26f, .25f, .63f), steel);
                aircraft.Ellipsoid(new Vector3(side * 3.04f, .1f, -1.19f), Vector3.one * .14f, side > 0 ? green : amber);
            }
            aircraft.Beam(new Vector3(0f, .17f, -2f), new Vector3(0f, 1.2f, -2.68f), .26f, steel);
            Model("Airliner body and navigation lamps", plane, aircraft);

            var ship = new Batch();
            ship.Ellipsoid(Vector3.zero, new Vector3(2.85f, .45f, 2.85f), steel);
            ship.Ellipsoid(Vector3.up * -.25f, new Vector3(2.06f, .42f, 2.06f), dark);
            ship.Ellipsoid(Vector3.up * .44f, new Vector3(1.09f, .59f, .99f), dark);
            ship.Ellipsoid(new Vector3(0f, .82f, .68f), new Vector3(.41f, .47f, .31f), green);
            for (int side = -1; side <= 1; side += 2)
                ship.Ellipsoid(new Vector3(side * .18f, .90f, .944f), new Vector3(.105f, .141f, .072f), dark);
            for (int i = 0; i < 10; i++)
            {
                float angle = i * Mathf.PI * .2f;
                ship.Ellipsoid(new Vector3(Mathf.Cos(angle) * 2.42f, .14f, Mathf.Sin(angle) * 2.42f),
                    new Vector3(.18f, .13f, .18f), cyan);
            }
            Model("Saucer with alien pilot", saucer, ship);
        }

        private static Transform Group(string name, Transform parent)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private void Model(string name, Transform parent, Batch batch)
        {
            Transform model = Group(name, parent);
            var mesh = batch.CreateMesh(name);
            resources.Add(mesh);
            model.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = model.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = batch.Materials.ToArray();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private Material Paint(string name, int rgb)
        {
            var material = new Material(shader)
            {
                name = name,
                color = new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255)
            };
            resources.Add(material);
            return material;
        }

        private Material LitPaint(string name, Color tint, float metallic)
        {
            var material = new Material(Shader.Find("Standard"))
            {
                name = name,
                color = tint
            };
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", .24f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", tint * .16f);
            resources.Add(material);
            return material;
        }

        private Material PlanetPaint()
        {
            var material = LitPaint("Ringed planet atmosphere", Color.white, 0f);
            var texture = new Texture2D(128, 64, TextureFormat.RGBA32, true)
            {
                name = "Planet atmospheric bands",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear
            };
            var pixels = new Color32[128 * 64];
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 128; x++)
            {
                float angle = x * Mathf.PI * 2f / 128f;
                float noise = Mathf.PerlinNoise(Mathf.Sin(angle) * 1.6f + 4f, y * .14f + Mathf.Cos(angle));
                float belt = .5f + Mathf.Sin(y * .57f + noise * 1.7f) * .31f + noise * .14f;
                pixels[y * 128 + x] = Color.Lerp(new Color(.16f, .28f, .43f), new Color(.66f, .78f, .86f), belt);
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            resources.Add(texture);
            material.mainTexture = texture;
            material.SetTexture("_EmissionMap", texture);
            material.SetColor("_EmissionColor", new Color(.27f, .35f, .46f));
            return material;
        }

        internal void Dispose()
        {
            foreach (Object resource in resources)
                Object.Destroy(resource);
        }
    }
}
