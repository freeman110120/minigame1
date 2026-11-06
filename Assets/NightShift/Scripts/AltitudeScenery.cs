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
            // All three terraces share one processional axis, receding into the clouds.
            Transform palace = Group("Three terraces of the cloud palace", heaven);
            palace.localPosition = new Vector3(-17f, 46f, 18f);
            palace.localRotation = Quaternion.Euler(0f, -34f, 0f);

            var lower = new Batch();
            Terrace(lower, 0f, 0f, 15.8f, 9.6f);
            Hall(lower, new Vector3(0f, .2f, -1.9f), 8.1f, 3.6f, 2.8f, 4, false);
            for (int side = -1; side <= 1; side += 2)
            {
                Lantern(lower, new Vector3(side * 5.6f, .2f, -3.1f));
                Lantern(lower, new Vector3(side * 5.6f, .2f, 2.9f));
            }
            Model("Lower terrace and ceremonial gate", palace, lower);

            var middle = new Batch();
            Terrace(middle, 7.2f, 16f, 19.6f, 11f);
            for (int side = -1; side <= 1; side += 2)
            {
                Hall(middle, new Vector3(side * 5.75f, 7.4f, 16.2f), 5.9f, 4.5f, 3.3f, 3, true);
                Lantern(middle, new Vector3(side * 2.3f, 7.4f, 13.3f));
                Lantern(middle, new Vector3(side * 2.3f, 7.4f, 19.6f));
            }
            Model("Middle court and paired golden pavilions", palace, middle);

            var upper = new Batch();
            Terrace(upper, 16.4f, 35f, 21.4f, 13.4f);
            Hall(upper, new Vector3(0f, 17.1f, 36.1f), 12.5f, 6.1f, 4.35f, 6, true);
            for (int side = -1; side <= 1; side += 2)
            {
                Hall(upper, new Vector3(side * 8.1f, 16.6f, 32f), 3.5f, 3.4f, 2.8f, 2, true);
                Lantern(upper, new Vector3(side * 3.6f, 16.6f, 30.8f));
            }
            upper.Box(new Vector3(0f, 16.66f, 33.2f), new Vector3(7.2f, .5f, 5.1f), ivory);
            Model("Upper sanctuary and crowned main hall", palace, upper);

            var stairs = new Batch();
            Stairway(stairs, 4.4f, .17f, 10.85f, 7.37f, 4.2f);
            Stairway(stairs, 21.1f, 7.37f, 28.85f, 16.57f, 4.8f);
            Model("Golden stair bridges between terraces", palace, stairs);
        }

        private void Terrace(Batch mesh, float y, float z, float width, float depth)
        {
            mesh.Box(new Vector3(0f, y - .2f, z), new Vector3(width, .55f, depth), ivory);
            mesh.Box(new Vector3(0f, y - .53f, z), new Vector3(width + .35f, .16f, depth + .28f), gold);
            mesh.Box(new Vector3(0f, y - .91f, z), new Vector3(width - .95f, .66f, depth - .75f), mist);
            mesh.Box(new Vector3(0f, y - 1.43f, z), new Vector3(width - 2.65f, .42f, depth - 1.65f), ivory);
            mesh.Box(new Vector3(0f, y + .09f, z), new Vector3(3.6f, .042f, depth - .1f), gold);
            for (int i = 0; i < 7; i++)
            {
                float x = (i - 3f) * width * .135f;
                mesh.Ellipsoid(new Vector3(x, y - 2.05f - i % 3 * .24f, z + (i % 2 - .5f) * depth * .46f),
                    new Vector3(width * .17f, 1.06f + i % 3 * .23f, depth * .33f), i % 2 == 0 ? mist : ivory, 16, 8);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (width * .5f - .27f);
                Balustrade(mesh, new Vector3(x, y + .09f, z - depth * .5f + .26f),
                    new Vector3(x, y + .09f, z + depth * .5f - .26f), 6);
                for (int edge = -1; edge <= 1; edge += 2)
                {
                    float edgeZ = z + edge * (depth * .5f - .25f);
                    Balustrade(mesh, new Vector3(side * 2.55f, y + .09f, edgeZ),
                        new Vector3(x, y + .09f, edgeZ), 4);
                }
            }
        }

        private void Hall(Batch mesh, Vector3 origin, float width, float depth, float height, int columns, bool doubleRoof)
        {
            mesh.Box(origin + Vector3.down * .09f, new Vector3(width + .43f, .18f, depth + .43f), gold);
            for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < columns; i++)
            {
                float x = Mathf.Lerp(-width * .43f, width * .43f, i / (columns - 1f));
                Vector3 foot = origin + new Vector3(x, 0f, side * depth * .4f);
                mesh.Box(foot + Vector3.up * .11f, new Vector3(.56f, .22f, .56f), gold);
                mesh.Box(foot + Vector3.up * height * .5f, new Vector3(.35f, height, .35f), ivory);
                mesh.Box(foot + Vector3.up * (height - .16f), new Vector3(.59f, .25f, .59f), amber);
            }
            mesh.Box(origin + Vector3.up * (height - .14f), new Vector3(width, .27f, depth), gold);
            if (width >= 10f)
            {
                mesh.Box(origin + new Vector3(0f, height * .47f, depth * .32f),
                    new Vector3(width * .79f, height * .94f, .22f), gold);
                for (int door = -2; door <= 2; door++)
                {
                    Vector3 panel = origin + new Vector3(door * width * .145f, height * .39f, depth * .32f - .13f);
                    mesh.Box(panel, new Vector3(.92f, height * .76f, .055f), ivory);
                    mesh.Box(panel + new Vector3(0f, 0f, -.042f), new Vector3(.58f, height * .67f, .045f), gold);
                    mesh.Ellipsoid(panel + new Vector3(0f, .08f, -.08f), new Vector3(.11f, .16f, .036f), amber);
                }
            }
            Roof(mesh, origin + Vector3.up * height, width, depth);
            if (doubleRoof)
            {
                mesh.Box(origin + Vector3.up * (height + .97f), new Vector3(width * .65f, .56f, depth * .55f), ivory);
                Roof(mesh, origin + Vector3.up * (height + 1.28f), width * .74f, depth * .7f);
                if (width >= 10f)
                {
                    Vector3 crown = origin + Vector3.up * (height + 2.63f);
                    mesh.Ellipsoid(crown, new Vector3(.45f, .36f, .45f), gold);
                    mesh.Beam(crown, crown + Vector3.up * .87f, .092f, amber);
                    mesh.Ellipsoid(crown + Vector3.up * .87f, Vector3.one * .14f, amber);
                }
            }
        }

        private void Roof(Batch mesh, Vector3 basePoint, float width, float depth)
        {
            mesh.Box(basePoint, new Vector3(width + .88f, .15f, depth + .94f), amber);
            for (int tier = 0; tier < 5; tier++)
            {
                float amount = (tier + 1f) / 5f;
                mesh.Box(basePoint + Vector3.up * (.13f + amount * .71f),
                    new Vector3(width + .76f - amount * .69f, .18f, depth * (1f - amount * .91f) + .59f),
                    tier == 4 ? amber : gold);
            }
            int ribs = Mathf.FloorToInt(width / 1.15f);
            for (int rib = -ribs / 2; rib <= ribs / 2; rib++)
            {
                Vector3 ridge = basePoint + new Vector3(rib * .92f, .96f, 0f);
                for (int end = -1; end <= 1; end += 2)
                    mesh.Beam(ridge, basePoint + new Vector3(rib * .92f, .14f, end * (depth * .5f + .38f)),
                        .057f, amber);
            }
            for (int side = -1; side <= 1; side += 2)
            for (int end = -1; end <= 1; end += 2)
            {
                Vector3 corner = basePoint + new Vector3(side * (width * .5f + .32f), .05f, end * (depth * .5f + .37f));
                mesh.Beam(corner, corner + new Vector3(side * .52f, .37f, end * .12f), .12f, gold);
                mesh.Ellipsoid(corner + new Vector3(side * .52f, .37f, end * .12f), Vector3.one * .12f, amber);
            }
            mesh.Ellipsoid(basePoint + Vector3.up * .98f, new Vector3(.24f, .28f, .24f), amber);
        }

        private void Stairway(Batch mesh, float fromZ, float fromY, float toZ, float toY, float width)
        {
            const int steps = 22;
            float tread = (toZ - fromZ) / steps;
            float rise = (toY - fromY) / steps;
            for (int i = 0; i < steps; i++)
            {
                var point = new Vector3(0f, fromY + rise * (i + .5f), fromZ + tread * (i + .5f));
                mesh.Box(point - Vector3.up * .1f, new Vector3(width, rise + .24f, tread + .04f), ivory);
                mesh.Box(point + new Vector3(0f, rise * .5f + .025f, -.12f),
                    new Vector3(width + .035f, .067f, .11f), gold);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * (width * .5f - .11f);
                Balustrade(mesh, new Vector3(x, fromY, fromZ), new Vector3(x, toY, toZ), 9);
            }
        }

        private void Balustrade(Batch mesh, Vector3 start, Vector3 end, int posts)
        {
            Vector3 top = Vector3.up * .63f;
            mesh.Beam(start + top, end + top, .092f, gold);
            for (int i = 0; i <= posts; i++)
            {
                Vector3 point = Vector3.Lerp(start, end, i / (float)posts);
                mesh.Box(point + top * .49f, new Vector3(.105f, .63f, .105f), ivory);
                mesh.Box(point + top, new Vector3(.15f, .12f, .15f), amber);
            }
        }

        private void Lantern(Batch mesh, Vector3 point)
        {
            mesh.Box(point + Vector3.up * .34f, new Vector3(.14f, .68f, .14f), gold);
            mesh.Box(point + Vector3.up * .78f, new Vector3(.27f, .28f, .27f), amber);
            mesh.Box(point + Vector3.up * .96f, new Vector3(.39f, .095f, .39f), gold);
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
