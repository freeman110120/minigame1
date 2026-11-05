using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NightShift
{
    internal sealed class NightShiftArt
    {
        private const float FloorWidth = 5.2f;
        private const float FloorHeight = 1.2f;
        private const float WindowBayWidth = .816f;
        private const float CrewWindowOffset = -.5f * WindowBayWidth;
        private const int CrewFloor = 1;
        private const int EquipmentFloor = 2;
        private const string WindowCrewName = "Window crew";
        private const float YardWidth = 19f;
        private const float StreetWidth = 110f;
        private const float AsphaltTileWidth = 5.5f;
        private static readonly Color YardBase = Hex("2D3845");
        private static readonly Color AsphaltTint = Hex("B9C9DB");
        private static readonly Color YardHighlight = Hex("C8894D");

        private readonly List<Object> resources = new List<Object>();
        private readonly Shader surfaceShader;
        private readonly Shader unlitShader;
        private readonly Material concrete;
        private readonly Material glass;
        private readonly Material warmGlass;
        private readonly Material idleGlass;
        private readonly Material crewGlass;
        private readonly Material equipmentGlass;
        private readonly Material mullion;
        private readonly Material flashing;
        private readonly Material roof;
        private readonly Material cranePaint;
        private readonly Material trafficPaint;
        private readonly Material asphalt;
        private readonly Material yardSurface;
        private readonly Material cityWall;
        private readonly Material cityGlass;
        private readonly Material cityWindow;
        private readonly Mesh[] floorMeshes = new Mesh[3];
        private readonly Mesh[] workerFrames = new Mesh[8];
        private readonly Material[] workerMaterials;
        private readonly Material[] suspendedCrewFloor;
        private readonly Mesh craneMesh;
        private readonly Mesh trolleyMesh;
        private readonly Mesh hookMesh;
        private readonly Material[][] darkFloor = new Material[3][];
        private readonly Material[][] brightFloor = new Material[3][];
        private readonly Material[] craneMaterials;
        private readonly Material[] trolleyMaterials;
        private readonly Material[] hookMaterials;
        private readonly Cubemap reflection;

        internal Transform CityRoot { get; }
        internal Material CableMaterial { get; }
        internal Material GuideMaterial { get; }

        internal NightShiftArt(Transform parent, Shader surfaceShader, Shader unlitShader, Texture2D wetAsphalt = null,
            Texture2D rainConcrete = null, Texture2D windowRoom = null)
        {
            this.surfaceShader = surfaceShader ? surfaceShader : Shader.Find("Standard");
            this.unlitShader = unlitShader ? unlitShader : Shader.Find("Unlit/Color");
            Color fallbackConcrete = Hex("96918B");
            Texture2D concreteTexture = rainConcrete ? rainConcrete : PaintTexture("Concrete grain", 256, (u, v) =>
            {
                float aggregate = Mathf.PerlinNoise(u * 28f + 8f, v * 28f + 3f);
                float pores = Mathf.PerlinNoise(u * 92f, v * 92f);
                float mottle = Mathf.PerlinNoise(u * 5f + 2f, v * 5f + 6f);
                return fallbackConcrete * (.58f + .24f * mottle + .12f * aggregate + .12f * pores);
            }, anisoLevel: 8, wrapMode: TextureWrapMode.Repeat);
            concrete = Surface("Rain-stained precast", Hex("D7D1C6"), 0f, .27f);
            concrete.mainTexture = concreteTexture;
            roof = Surface("Wet concrete deck", Hex("C6CBD0"), 0f, .56f);
            roof.mainTexture = concreteTexture;

            Texture2D room = windowRoom ? windowRoom : PaintTexture("Unfinished interior", 128, PaintRoom);
            glass = RoomGlass("Rainy blue glazing", room, Hex("475F71"), Hex("1B2933"), .65f);
            idleGlass = RoomGlass("Unfinished room under work light", room, Hex("83776C"), Hex("E9BA8C") * .45f, .53f);
            warmGlass = RoomGlass("Amber-lit construction room", room, Hex("A3988C"), Hex("FFDBB8") * .90f, .53f);
            crewGlass = RoomGlass("Backlit crew room", PaintTexture("Crew room with work lights", 96, PaintCrewRoom),
                Hex("B49B80"), Hex("FFC489") * .58f, .44f);
            equipmentGlass = RoomGlass("Cool equipment room", PaintTexture("Equipment room cabinets", 96, PaintEquipmentRoom),
                Hex("8597A3"), Hex("BCD9F3") * .46f, .47f);
            mullion = Surface("Dark weathered steel", Hex("30383C"), .18f, .47f);
            flashing = Surface("Wet galvanized edge flashing", Hex("52606A"), .25f, .56f);
            cranePaint = Surface("Weathered ochre", Hex("C58B3F"), .18f, .46f);
            trafficPaint = Surface("Worn amber site marking", Hex("AC864D"), 0f, .42f);
            Texture2D asphaltSource = wetAsphalt && wetAsphalt.isReadable ? wetAsphalt : null;
            asphalt = Surface("Rain-darkened street", asphaltSource ? AsphaltTint : YardBase, 0f, .71f);
            if (asphaltSource)
            {
                asphalt.mainTexture = asphaltSource;
                float tiles = StreetWidth / AsphaltTileWidth;
                asphalt.mainTextureScale = Vector2.one * tiles;
                asphalt.mainTextureOffset = Vector2.one * (.5f - tiles * .5f);
            }
            yardSurface = Surface("Damp construction yard", Color.white, .08f, .80f);
            yardSurface.mainTexture = PaintTexture("Asphalt with broken lamp reflections", 768,
                (u, v) => PaintYard(u, v, asphaltSource), anisoLevel: 8);
            cityWall = Surface("Weather-stained blue-grey masonry", Hex("29343F"), 0f, .32f);
            cityWall.mainTexture = PaintTexture("Masonry grain and rain streaks", 256, (u, v) =>
            {
                float mineral = Mathf.PerlinNoise(u * 18f + 8f, v * 28f + 5f);
                float streak = Mathf.PerlinNoise(u * 38f + 7f, v * 3.5f + 2f);
                float moisture = Mathf.PerlinNoise(u * 4.5f + 1f, v * 6.5f + 8f);
                return Color.white * (.68f + .12f * mineral + .21f * streak + .13f * moisture);
            });
            cityGlass = Surface("Distant blue glass", Hex("27343F"), .12f, .54f);
            cityGlass.mainTexture = PaintTexture("Sky reflected in distant windows", 64, (u, v) =>
            {
                float reflectedSky = Mathf.SmoothStep(.56f, 1f, v);
                float skyline = Mathf.PerlinNoise(u * 7f + 2f, v * 2f + 9f);
                return Color.white * (reflectedSky * (.82f + .18f * skyline));
            });
            cityWindow = Surface("Distant occupied rooms", Hex("594D3B"), 0f, .38f, Hex("E4AC76") * .18f);
            CableMaterial = Surface("Blackened hoist cable", Hex("52616D"), .42f, .49f);
            GuideMaterial = Own(new Material(this.unlitShader) { name = "Warm landing guide", color = Hex("FFD792") });

            for (int i = 0; i < floorMeshes.Length; i++)
            {
                MeshBatch floor = BuildFloor(i);
                floorMeshes[i] = Own(floor.CreateMesh("Precast floor cassette " + i,
                    new Vector3(1f / FloorWidth, 1f / FloorHeight, 1f / FloorWidth)));
                brightFloor[i] = floor.Materials.ToArray();
                darkFloor[i] = (Material[])brightFloor[i].Clone();
                darkFloor[i][Array.IndexOf(darkFloor[i], warmGlass)] = idleGlass;
            }
            suspendedCrewFloor = (Material[])darkFloor[CrewFloor].Clone();
            suspendedCrewFloor[Array.IndexOf(suspendedCrewFloor, crewGlass)] = glass;
            var silhouette = Own(new Material(this.unlitShader) { name = "Backlit worker silhouette", color = Hex("22282D") });
            workerMaterials = new[] { silhouette };
            for (int i = 0; i < workerFrames.Length; i++)
                workerFrames[i] = Own(BuildWorkerFrame(i * Mathf.PI * 2f / workerFrames.Length, silhouette)
                    .CreateMesh("Worker gait frame " + i));

            MeshBatch crane = BuildCrane();
            craneMesh = Own(crane.CreateMesh("Lattice crane jib"));
            craneMaterials = crane.Materials.ToArray();
            MeshBatch trolley = BuildTrolley();
            trolleyMesh = Own(trolley.CreateMesh("Hoist carriage"));
            trolleyMaterials = trolley.Materials.ToArray();
            MeshBatch hook = BuildHook();
            hookMesh = Own(hook.CreateMesh("Striped load hook"));
            hookMaterials = hook.Materials.ToArray();
            CityRoot = Group("Rainy construction district", parent);
            BuildCity();

            reflection = MakeReflection();
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("8997A7") * .59f;
            RenderSettings.ambientEquatorColor = Hex("6A7682") * .55f;
            RenderSettings.ambientGroundColor = Hex("3D454F") * .50f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = reflection;
            RenderSettings.reflectionIntensity = .68f;
            AddLight(parent, "Cool night skylight", LightType.Directional, Vector3.zero,
                Quaternion.Euler(38f, -46f, 0f), Hex("D2E0F1"), .76f, 0f, LightShadows.Soft);
            AddLight(parent, "Soft work light above tower", LightType.Directional, Vector3.zero,
                Quaternion.Euler(34f, 137f, 0f), Hex("F6D4AD"), .24f, 0f, LightShadows.None);
        }

        internal GameObject CreateFloor(Vector3 bottom, Vector2 size, float height, Transform parent, int level)
        {
            int variant = level % floorMeshes.Length;
            Material[] materials = variant == CrewFloor ? suspendedCrewFloor : darkFloor[variant];
            MeshRenderer renderer = Model("Precast curtain-wall floor", parent, floorMeshes[variant], materials);
            renderer.transform.position = bottom;
            renderer.transform.localScale = new Vector3(size.x, height, size.y);
            if (variant == CrewFloor)
            {
                Transform crew = Group(WindowCrewName, renderer.transform);
                crew.gameObject.SetActive(false);
                AddWindowWorker(crew, Vector3.back, Vector3.right, level * .37f);
                AddWindowWorker(crew, Vector3.right, Vector3.forward, level * .37f + 1.23f);
            }
            return renderer.gameObject;
        }

        internal void SetFloorLit(GameObject floor, bool lit)
        {
            int variant = Array.IndexOf(floorMeshes, floor.GetComponent<MeshFilter>().sharedMesh);
            floor.GetComponent<MeshRenderer>().sharedMaterials = lit ? brightFloor[variant] : darkFloor[variant];
            if (variant == CrewFloor)
                floor.transform.Find(WindowCrewName).gameObject.SetActive(true);
        }

        private void AddWindowWorker(Transform parent, Vector3 outward, Vector3 along, float phase)
        {
            var window = Group("Window", parent);
            Vector3 position = outward * 2.522f + along * CrewWindowOffset + Vector3.up * .254f;
            window.localPosition = new Vector3(position.x / FloorWidth, position.y / FloorHeight, position.z / FloorWidth);
            window.localRotation = Quaternion.LookRotation(outward);
            // Keep the silhouette near the glazing even when seen from the side.
            window.localScale = new Vector3(1f / FloorWidth, 1f / FloorHeight, .15f / FloorWidth);
            MeshRenderer worker = Model("Walking worker", window, workerFrames[0], workerMaterials);
            worker.shadowCastingMode = ShadowCastingMode.Off;
            worker.receiveShadows = false;
            worker.gameObject.AddComponent<NightShiftWindowWorker>().Initialize(worker, workerFrames, phase);
        }

        private static MeshBatch BuildWorkerFrame(float step, Material silhouette)
        {
            var shape = new MeshBatch();
            float stride = Mathf.Sin(step) * .081f;
            float rise = Mathf.Abs(Mathf.Cos(step)) * .015f;
            Vector3 hips = new Vector3(0f, .257f + rise, 0f);
            Vector3 shoulders = new Vector3(.018f, .432f + rise, 0f);
            shape.Beam(hips, shoulders, .092f, silhouette);
            shape.Box(shoulders + new Vector3(.025f, .065f, 0f), new Vector3(.075f, .096f, .078f), silhouette);
            shape.Box(shoulders + new Vector3(.028f, .101f, 0f), new Vector3(.102f, .036f, .088f), silhouette);
            for (int side = -1; side <= 1; side += 2)
            {
                float swing = stride * side;
                float depth = side * .018f;
                Vector3 foot = new Vector3(swing + .014f, .033f + Mathf.Max(0f, Mathf.Cos(step) * side) * .033f, depth);
                Vector3 knee = new Vector3(swing * .72f + .028f, .146f, depth);
                Vector3 elbow = new Vector3(.009f - swing * .47f, .353f + rise, depth);
                Vector3 hand = new Vector3(.022f - swing * .83f, .278f + rise, depth);
                shape.Beam(hips, knee, .041f, silhouette);
                shape.Beam(knee, foot, .037f, silhouette);
                shape.Box(foot + Vector3.right * .014f, new Vector3(.058f, .030f, .045f), silhouette);
                shape.Beam(shoulders, elbow, .035f, silhouette);
                shape.Beam(elbow, hand, .027f, silhouette);
            }
            return shape;
        }

        internal GameObject CreateCrane(Transform parent)
        {
            MeshRenderer jib = Model("Ochre crane jib", parent, craneMesh, craneMaterials);
            MeshRenderer trolley = Model("Hoist carriage", jib.transform, trolleyMesh, trolleyMaterials);
            Light workLight = AddLight(trolley.transform, "Crane floodlight", LightType.Spot,
                new Vector3(.32f, .02f, -.41f), Quaternion.LookRotation(new Vector3(-.3f, -3.5f, .55f)),
                Hex("FFD1A1"), 2.05f, 10f, LightShadows.None);
            workLight.spotAngle = 101f;
            workLight.innerSpotAngle = 61f;
            return jib.gameObject;
        }

        internal void SetTrolley(GameObject crane, float localX)
        {
            crane.transform.GetChild(0).localPosition = Vector3.right * localX;
        }

        internal Transform CreateHook(Transform parent) => Model("Load hook", parent, hookMesh, hookMaterials).transform;

        internal void Dispose()
        {
            if (RenderSettings.customReflectionTexture == reflection)
                RenderSettings.customReflectionTexture = null;

            foreach (Object resource in resources)
                Object.Destroy(resource);
        }

        private MeshBatch BuildFloor(int variant)
        {
            const int bays = 6;
            const float bay = WindowBayWidth;
            const float concreteTile = 1.74f;
            var b = new MeshBatch();
            b.Slab(.014f, .153f, FloorWidth, .023f, concrete, concreteTile);
            b.Box(new Vector3(0f, .010f, 0f), new Vector3(5.04f, .020f, 5.04f), mullion);
            b.Box(new Vector3(0f, .59f, 0f), new Vector3(4.94f, .91f, 4.94f), mullion);
            b.Slab(1.012f, 1.197f, FloorWidth, .034f, concrete, concreteTile);
            b.Box(new Vector3(0f, 1.196f, 0f), new Vector3(5.13f, .006f, 5.13f), roof, concreteTile);
            for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
                b.Box(new Vector3(x * 2.48f, .583f, z * 2.48f), new Vector3(.19f, .91f, .19f), concrete, concreteTile);

            for (int side = 0; side < 4; side++)
            {
                Vector3 outward = side % 2 == 0 ? Vector3.forward : Vector3.right;
                if (side < 2)
                    outward = -outward;
                Vector3 across = Vector3.Cross(Vector3.up, outward);
                for (int i = 0; i < bays; i++)
                {
                    float offset = (i - (bays - 1) * .5f) * bay;
                    int pattern = i * 7 + side * 3 + variant * 4;
                    bool occupied = pattern % 9 < 4;
                    bool mirrored = pattern % 2 == 0;
                    float crop = .035f + .052f * (pattern % 4);
                    var roomUv = new Rect(mirrored ? crop + .78f : crop, .046f, mirrored ? -.78f : .78f, .90f);
                    Vector3 center = outward * 2.512f + (side % 2 == 0 ? Vector3.right : Vector3.forward) * offset;
                    Material pane = occupied ? warmGlass : glass;
                    bool crewWindow = variant == CrewFloor && i == 2 && (side == 0 || side == 3);
                    bool equipmentWindow = variant == EquipmentFloor && (i == 1 || i == 2 || i == 3);
                    if (crewWindow || equipmentWindow)
                    {
                        pane = crewWindow ? crewGlass : equipmentGlass;
                        roomUv = new Rect(0f, 0f, 1f, 1f);
                    }
                    b.Pane(center + Vector3.up * .535f, across * (bay * .5f), .335f, pane, roomUv);
                    b.Pane(center + Vector3.up * .941f, across * (bay * .5f), .072f, glass,
                        new Rect(crop, .78f, .78f, .16f));

                    if (i == bays - 1)
                        continue;

                    bool pier = i == 1 || i == 3;
                    Face(b, side, offset + bay * .5f, .584f, pier ? 2.529f : 2.52f,
                        pier ? .096f : .033f, .863f, pier ? .084f : .037f,
                        pier ? concrete : mullion, pier ? concreteTile : 0f);
                }
                Face(b, side, 0f, .871f, 2.529f, 4.968f, .035f, .047f, mullion);
                Face(b, side, 0f, 1.006f, 2.54f, 5.01f, .035f, .055f, flashing);
                Face(b, side, 0f, .183f, 2.54f, 5.01f, .045f, .068f, flashing);
            }

            b.Box(new Vector3(-.72f + .25f * variant, 1.199f, 0f), new Vector3(.018f, .002f, 5.04f), mullion);
            b.Box(new Vector3(0f, 1.199f, .74f - .37f * variant), new Vector3(5.04f, .002f, .015f), mullion);
            for (int side = 0; side < 4; side++)
                Face(b, side, .66f - .52f * variant, 1.199f, 2.39f, .24f, .002f, .092f, flashing);
            return b;
        }

        private static void Face(MeshBatch b, int side, float along, float y, float radius,
            float width, float height, float depth, Material material, float tileWidth = 0f)
        {
            float offset = side < 2 ? -radius : radius;
            if (side % 2 == 0)
                b.Box(new Vector3(along, y, offset), new Vector3(width, height, depth), material, tileWidth);
            else
                b.Box(new Vector3(offset, y, along), new Vector3(depth, height, width), material, tileWidth);
        }

        private MeshBatch BuildCrane()
        {
            const float lower = .18f;
            const float upper = 1.02f;
            const float halfSpan = .375f;
            const float start = -5.1f;
            const float step = 1.28f;
            const int sections = 14;
            const int rearSections = 4;
            var b = new MeshBatch();
            float end = start + sections * step;
            float mast = start + rearSections * step;

            b.Box(new Vector3((start + mast) * .5f, .17f, 0f),
                new Vector3(mast - start, .25f, .37f), mullion);
            b.Beam(new Vector3(mast, upper, 0f), new Vector3(end, upper, 0f), .10f, cranePaint);
            b.Beam(new Vector3(start, .025f, 0f), new Vector3(end, .025f, 0f), .106f, flashing);
            for (int side = -1; side <= 1; side += 2)
            {
                float z = halfSpan * side;
                b.Beam(new Vector3(mast, lower, z), new Vector3(end, lower, z), .105f, cranePaint);
                for (int i = rearSections; i < sections; i++)
                {
                    float x = start + i * step;
                    Vector3 peak = new Vector3(x + step * .5f, upper, 0f);
                    b.Beam(new Vector3(x, lower, z), peak, .075f, cranePaint);
                    b.Beam(peak, new Vector3(x + step, lower, z), .075f, cranePaint);
                    b.Beam(new Vector3(x, lower, -z), new Vector3(x + step, lower, z), .055f, cranePaint);
                }
            }
            for (int i = rearSections; i <= sections; i++)
            {
                float x = start + i * step;
                b.Beam(new Vector3(x, lower, -halfSpan), new Vector3(x, lower, halfSpan), .074f, cranePaint);
            }
            b.Box(new Vector3(end - .28f, lower, 0f), new Vector3(.78f, .26f, .88f), cranePaint);
            return b;
        }

        private MeshBatch BuildTrolley()
        {
            var b = new MeshBatch();
            b.Box(new Vector3(0f, .028f, 0f), new Vector3(.84f, .235f, .93f), CableMaterial);
            b.Box(new Vector3(.027f, -.101f, 0f), new Vector3(.35f, .065f, .42f), flashing);
            b.Box(new Vector3(-.08f, .037f, -.475f), new Vector3(.49f, .173f, .046f), cranePaint);
            for (int i = 0; i < 3; i++)
                b.Box(new Vector3(-.23f + i * .14f, .037f, -.504f), new Vector3(.046f, .157f, .009f),
                    mullion, Quaternion.Euler(0f, 0f, -29f));
            b.Box(new Vector3(.32f, .119f, -.40f), new Vector3(.38f, .17f, .27f), flashing);
            b.Box(new Vector3(.32f, .022f, -.41f), new Vector3(.32f, .024f, .23f), GuideMaterial);
            return b;
        }

        private MeshBatch BuildHook()
        {
            var b = new MeshBatch();
            b.Box(new Vector3(0f, .19f, 0f), new Vector3(.46f, .31f, .32f), cranePaint);
            b.Box(new Vector3(0f, .35f, 0f), new Vector3(.31f, .07f, .26f), mullion);
            for (int i = -1; i <= 1; i++)
                b.Box(new Vector3(i * .125f, .19f, -.163f), new Vector3(.051f, .245f, .016f), mullion,
                    Quaternion.Euler(0f, 0f, -31f));
            b.Beam(new Vector3(0f, .047f, 0f), new Vector3(0f, -.037f, 0f), .097f, flashing);
            b.Beam(new Vector3(0f, -.037f, 0f), new Vector3(.082f, -.094f, 0f), .066f, flashing);
            b.Beam(new Vector3(.082f, -.094f, 0f), new Vector3(.13f, -.039f, 0f), .060f, flashing);
            return b;
        }

        private void BuildCity()
        {
            var ground = new MeshBatch();
            ground.Box(new Vector3(0f, -.17f, 0f), new Vector3(StreetWidth, .22f, StreetWidth), asphalt);
            ground.Box(new Vector3(0f, -.035f, 0f), new Vector3(YardWidth, .07f, YardWidth), yardSurface);
            AddBatch("Street and wet construction apron", CityRoot, ground);

            var west = new MeshBatch();
            Building(west, -17.6f, -4.5f, 7.2f, 6.5f, 8.1f, 4, 5);
            Building(west, -22.4f, 4.6f, 7.8f, 7.9f, 14.6f, 7, 5);
            Building(west, -16.3f, 14.5f, 7.8f, 7.6f, 10.6f, 5, 5);
            Building(west, -28.5f, 9.4f, 7.6f, 9.2f, 21.9f, 11, 6);
            Building(west, -25.1f, 22f, 9.7f, 8.1f, 17.1f, 9, 6);
            AddBatch("West office blocks", CityRoot, west, false);

            var north = new MeshBatch();
            Building(north, -5.5f, 19.6f, 8f, 8f, 9.1f, 5, 5);
            Building(north, 4.2f, 23.4f, 7.8f, 8.9f, 13.1f, 7, 5);
            Building(north, 14.5f, 19.2f, 8.3f, 7.8f, 10.9f, 6, 5);
            Building(north, -9.3f, 28.1f, 8.6f, 7.9f, 20.1f, 10, 6);
            Building(north, 2.6f, 32.3f, 10.1f, 8.5f, 22.9f, 12, 7);
            Building(north, 18.1f, 31.7f, 9.4f, 9.4f, 18f, 9, 6);
            AddBatch("North and distant city blocks", CityRoot, north, false);

            var yard = new MeshBatch();
            Fence(yard, new Vector3(-7.2f, 0f, -5.5f), new Vector3(-7.2f, 0f, 7.4f), 6);
            Fence(yard, new Vector3(-7.2f, 0f, 7.4f), new Vector3(7.3f, 0f, 7.4f), 7);
            Fence(yard, new Vector3(7.3f, 0f, 1.1f), new Vector3(7.3f, 0f, 7.4f), 3);
            WorkLamp(yard, new Vector3(5.4f, 3.2f, -4.35f), Vector3.right);
            WorkLamp(yard, new Vector3(-5.7f, 3.2f, -3.8f), Vector3.left);
            WorkLamp(yard, new Vector3(-4.8f, 3.2f, 5.7f), Vector3.forward);
            Pallet(yard, new Vector3(5.65f, 0f, 2.8f), 12f, 3);
            Pallet(yard, new Vector3(6.3f, 0f, 4.2f), -19f, 0);
            Pallet(yard, new Vector3(-5.6f, 0f, -5.9f), -9f, 2);
            for (int sign = -1; sign <= 1; sign += 2)
            {
                float x = sign * 4.07f;
                for (int end = -1; end <= 1; end += 2)
                {
                    float z = end * 4.08f;
                    yard.Box(new Vector3(x - sign * .39f, .009f, z), new Vector3(.85f, .009f, .067f), trafficPaint);
                    yard.Box(new Vector3(x, .009f, z - end * .39f), new Vector3(.067f, .009f, .85f), trafficPaint);
                }
                for (int dash = 0; dash < 4; dash++)
                    yard.Box(new Vector3(sign * 2.22f, .009f, -5.08f - dash * 1.06f),
                        new Vector3(.067f, .009f, .55f), trafficPaint);
            }
            for (int i = 0; i < 4; i++)
                yard.Box(new Vector3(6.28f + i * .31f, .011f, -.2f), new Vector3(.11f, .011f, 1.02f),
                    trafficPaint, Quaternion.Euler(0f, -29f, 0f));
            AddBatch("Site fence, work lamps and service equipment", CityRoot, yard);
        }

        private void Building(MeshBatch b, float x, float z, float width, float depth, float height, int floors, int bays)
        {
            b.Box(new Vector3(x, .05f, z), new Vector3(width + .65f, .14f, depth + .61f), flashing);
            b.Box(new Vector3(x, height * .5f, z), new Vector3(width, height, depth), cityWall);
            b.Box(new Vector3(x, height + .075f, z), new Vector3(width + .16f, .15f, depth + .16f), cityWall);
            b.Box(new Vector3(x - width * .18f, height + .49f, z + depth * .12f),
                new Vector3(width * .35f, .98f, depth * .37f), cityWall);
            float story = height / (floors + .65f);
            float south = z - depth * .5f;
            float east = x + width * .5f;
            int seed = Mathf.RoundToInt(x * 37f + z * 53f);
            for (int level = 0; level < floors; level++)
            {
                float y = story * (level + .91f);
                b.Box(new Vector3(x, y, south - .019f), new Vector3(width * .84f, story * .46f, .05f), cityGlass);
                b.Box(new Vector3(east + .019f, y, z), new Vector3(.05f, story * .46f, depth * .84f), cityGlass);
                float sill = y - story * .25f;
                b.Box(new Vector3(x, sill, south - .023f), new Vector3(width * .88f, .071f, .11f), cityWall);
                b.Box(new Vector3(east + .023f, sill, z), new Vector3(.11f, .071f, depth * .88f), cityWall);
                for (int bay = 0; bay < bays; bay++)
                {
                    float along = ((bay + .5f) / bays - .5f) * .83f;
                    if (Mathf.Abs(seed + bay * 7 + level * 13) % 17 < 3)
                        b.Box(new Vector3(x + along * width, y, south - .05f),
                            new Vector3(width * .65f / bays, story * .35f, .019f), cityWindow);
                    if (Mathf.Abs(seed + bay * 13 + level * 7) % 23 < 4)
                        b.Box(new Vector3(east + .05f, y, z + along * depth),
                            new Vector3(.019f, story * .35f, depth * .65f / bays), cityWindow);
                }
            }
            for (int bay = 1; bay < bays; bay++)
            {
                float along = ((float)bay / bays - .5f) * .85f;
                b.Box(new Vector3(x + along * width, height * .49f, south - .01f),
                    new Vector3(.082f, height * .89f, .095f), cityWall);
                b.Box(new Vector3(east + .01f, height * .49f, z + along * depth),
                    new Vector3(.095f, height * .89f, .082f), cityWall);
            }
        }

        private void Fence(MeshBatch b, Vector3 start, Vector3 end, int sections)
        {
            for (int i = 0; i <= sections; i++)
            {
                Vector3 position = Vector3.Lerp(start, end, (float)i / sections);
                b.Box(position + Vector3.up * .077f, new Vector3(.26f, .154f, .26f), concrete);
                b.Box(position + Vector3.up * .87f, new Vector3(.086f, 1.74f, .086f), flashing);
            }
            for (int i = 1; i < sections * 7; i++)
            {
                Vector3 position = Vector3.Lerp(start, end, i / (sections * 7f));
                b.Box(position + Vector3.up * .9f, new Vector3(.024f, 1.29f, .024f), mullion);
            }
            foreach (float rail in new[] { .27f, 1.52f })
                b.Beam(start + Vector3.up * rail, end + Vector3.up * rail, .044f, flashing);
        }

        private void WorkLamp(MeshBatch b, Vector3 head, Vector3 outward)
        {
            Vector3 post = head + outward * .43f;
            b.Box(new Vector3(post.x, .09f, post.z), new Vector3(.49f, .18f, .49f), concrete);
            b.Beam(new Vector3(post.x, .15f, post.z), post, .078f, flashing);
            b.Beam(post, head, .063f, flashing);
            bool side = outward.x != 0f;
            b.Box(head, side ? new Vector3(.36f, .17f, .61f) : new Vector3(.61f, .17f, .36f), mullion);
            b.Box(head - Vector3.up * .096f,
                side ? new Vector3(.3f, .026f, .53f) : new Vector3(.53f, .026f, .3f), GuideMaterial);
            b.Box(head - outward * .19f - Vector3.up * .014f,
                side ? new Vector3(.026f, .105f, .48f) : new Vector3(.48f, .105f, .026f), GuideMaterial);
            Light lamp = AddLight(CityRoot, "Amber yard light", LightType.Spot, head - Vector3.up * .15f,
                Quaternion.LookRotation(-outward * .5f + Vector3.down), Hex("FFB372"), 2.65f, 9.5f, LightShadows.None);
            lamp.spotAngle = 94f;
            lamp.innerSpotAngle = 52f;
        }

        private void Pallet(MeshBatch b, Vector3 position, float angle, int panels)
        {
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            for (int i = -1; i <= 1; i++)
                b.Box(position + rotation * new Vector3(i * .32f, .055f, 0f),
                    new Vector3(.11f, .11f, .76f), trafficPaint, rotation);
            for (int i = -2; i <= 2; i++)
                b.Box(position + rotation * new Vector3(0f, .132f, i * .15f),
                    new Vector3(.84f, .045f, .12f), trafficPaint, rotation);
            for (int i = 0; i < panels; i++)
                b.Box(position + rotation * new Vector3(i * .025f, .215f + i * .13f, 0f),
                    new Vector3(.75f, .116f, .63f), concrete, rotation);
        }

        private static Color PaintRoom(float u, float v)
        {
            float grain = Mathf.PerlinNoise(u * 28f + 5f, v * 28f + 8f);
            float lamp = Mathf.Exp(-Mathf.Pow((u - .31f) * 3.5f, 2f) - Mathf.Pow((v - .59f) * 2.9f, 2f));
            float floor = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.21f + u * .06f, .36f, v));
            float concrete = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.74f, .85f, v));
            Color room = Color.Lerp(new Color(.17f, .20f, .22f), new Color(.64f, .41f, .23f), lamp * .86f);
            room = Color.Lerp(room, new Color(.18f, .15f, .12f), floor * .55f);
            room = Color.Lerp(room, new Color(.26f, .28f, .30f), concrete * .76f);
            if (u > .63f && u < .70f && v > .24f && v < .82f)
                room *= .63f;
            if (u > .24f && u < .37f && v > .69f && v < .72f)
                room = new Color(1f, .78f, .54f);
            return room * (.79f + .27f * grain);
        }

        private static Color PaintCrewRoom(float u, float v)
        {
            float litWall = Mathf.Exp(-Mathf.Pow((u - .49f) * 2.4f, 2f) - Mathf.Pow((v - .61f) * 2.1f, 2f));
            float grain = Mathf.PerlinNoise(u * 21f + 6f, v * 18f + 7f);
            Color room = Color.Lerp(new Color(.24f, .23f, .22f), new Color(.74f, .57f, .38f), litWall);
            if (v > .91f || v < .13f)
                room *= .60f;
            if (u > .17f && u < .83f && v > .86f && v < .91f)
                room = new Color(.94f, .79f, .57f);
            return room * (.88f + .20f * grain);
        }

        private static Color PaintEquipmentRoom(float u, float v)
        {
            float spill = Mathf.Exp(-Mathf.Pow((v - .77f) * 2.2f, 2f));
            Color room = Color.Lerp(new Color(.16f, .20f, .22f), new Color(.36f, .46f, .50f), spill);
            float cabinet = Mathf.Repeat(u * 3f, 1f);
            if (v > .14f && v < .73f && cabinet > .08f && cabinet < .91f)
            {
                room *= .70f;
                if (v > .37f && v < .61f && Mathf.Repeat(v * 35f, 1f) < .34f)
                    room *= .73f;
                if (v > .65f && v < .69f && cabinet > .20f && cabinet < .32f)
                    room = new Color(.47f, .70f, .64f);
            }
            if (u > .10f && u < .90f && v > .82f && v < .87f)
                room = new Color(.67f, .79f, .84f);
            return room;
        }

        private static Color PaintYard(float u, float v, Texture2D asphaltSource)
        {
            Vector2 point = new Vector2(u - .5f, .5f - v) * YardWidth;
            float aggregate = Mathf.PerlinNoise(u * 42f + 19f, v * 42f + 9f);
            float damp = Mathf.PerlinNoise(u * 9f + 2f, v * 9f + 5f);
            float edge = Mathf.Clamp01(Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) * 28f);
            Color pavement;
            if (asphaltSource)
            {
                Color sample = asphaltSource.GetPixelBilinear(.5f + point.x / AsphaltTileWidth,
                    .5f - point.y / AsphaltTileWidth);
                pavement = sample * AsphaltTint * Mathf.Lerp(1f, .92f + .16f * damp, edge);
            }
            else
            {
                float noise = Mathf.PerlinNoise(u * 173f, v * 173f);
                float tint = Mathf.Lerp(1f, .75f + .22f * noise + .13f * aggregate + .15f * damp, edge);
                pavement = YardBase * tint;
            }
            float ripple = Mathf.PerlinNoise(point.x * 7.4f + 20f, point.y * 12f + 49f);
            float reflected = Mathf.Clamp01((ripple * .68f + aggregate * .32f - .39f) * 1.65f);
            float spread = ReflectionStreak(point, new Vector2(5.2f, -5.6f), new Vector2(.97f, 3.4f), .30f)
                + ReflectionStreak(point, new Vector2(-4.6f, -4.9f), new Vector2(.89f, 2.75f), .26f)
                + ReflectionStreak(point, new Vector2(-3.7f, 4.8f), new Vector2(.8f, 2.24f), -.24f);
            float core = ReflectionStreak(point, new Vector2(5.18f, -6.01f), new Vector2(.38f, 2.8f), .30f)
                + ReflectionStreak(point, new Vector2(-4.61f, -5.25f), new Vector2(.30f, 2.12f), .26f);
            return Color.Lerp(pavement, YardHighlight, (spread * .68f + core * .41f) * reflected * edge);
        }

        private static float ReflectionStreak(Vector2 point, Vector2 center, Vector2 axes, float shear)
        {
            Vector2 delta = point - center;
            float x = (delta.x + delta.y * shear) / axes.x;
            float y = delta.y / axes.y;
            return Mathf.Pow(Mathf.Clamp01(1f - x * x - y * y), 1.75f);
        }

        private Cubemap MakeReflection()
        {
            const int resolution = 16;
            var cube = Own(new Cubemap(resolution, TextureFormat.RGBA32, true) { name = "Night skyline reflections" });
            var pixels = new Color[resolution * resolution];
            Color street = Hex("242A31");
            Color sky = Hex("8999AA");
            for (int face = 0; face < 6; face++)
            {
                for (int y = 0; y < resolution; y++)
                for (int x = 0; x < resolution; x++)
                {
                    float elevation = y / (resolution - 1f);
                    float value = face == (int)CubemapFace.PositiveY ? .81f
                        : face == (int)CubemapFace.NegativeY ? .11f : .22f + .44f * elevation;
                    if ((x + face * 3) % 7 < 2 && y < resolution / 2)
                        value -= .08f;
                    pixels[y * resolution + x] = Color.Lerp(street, sky, value);
                }
                cube.SetPixels(pixels, (CubemapFace)face);
            }
            cube.Apply(true);
            return cube;
        }

        private Material Surface(string name, Color color, float metallic, float smoothness, Color emission = default)
        {
            var material = Own(new Material(surfaceShader) { name = name, color = color });
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);
            if (emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
            return material;
        }

        private Material RoomGlass(string name, Texture2D texture, Color tint, Color emission, float smoothness)
        {
            Material material = Surface(name, tint, .12f, smoothness, emission);
            material.mainTexture = texture;
            material.SetTexture("_EmissionMap", texture);
            return material;
        }

        private Texture2D PaintTexture(string name, int resolution, Func<float, float, Color> paint,
            int anisoLevel = 4, TextureWrapMode wrapMode = TextureWrapMode.Clamp)
        {
            var texture = Own(new Texture2D(resolution, resolution, TextureFormat.RGBA32, true)
            {
                name = name, filterMode = FilterMode.Trilinear, wrapMode = wrapMode, anisoLevel = anisoLevel
            });
            var pixels = new Color32[resolution * resolution];
            for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
                pixels[y * resolution + x] = paint(x / (resolution - 1f), y / (resolution - 1f));
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

        private void AddBatch(string name, Transform parent, MeshBatch batch, bool castShadows = true)
        {
            var renderer = Model(name, parent, Own(batch.CreateMesh(name)), batch.Materials.ToArray());
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }

        private static Transform Group(string name, Transform parent)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static MeshRenderer Model(string name, Transform parent, Mesh mesh, Material[] materials)
        {
            GameObject model = Group(name, parent).gameObject;
            model.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = model.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            return renderer;
        }

        private static Light AddLight(Transform parent, string name, LightType type, Vector3 position,
            Quaternion rotation, Color color, float intensity, float range, LightShadows shadows)
        {
            Transform group = Group(name, parent);
            group.SetLocalPositionAndRotation(position, rotation);
            Light light = group.gameObject.AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.shadows = shadows;
            light.shadowStrength = .65f;
            if (type != LightType.Directional)
                light.range = range;
            return light;
        }

        private T Own<T>(T asset) where T : Object
        {
            resources.Add(asset);
            return asset;
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString("#" + value, out Color color);
            return color;
        }

        private sealed class MeshBatch
        {
            // Each entry supplies the right and up axes of a visible side.
            private static readonly Vector3[] FaceRight =
            {
                Vector3.right, Vector3.left, Vector3.back, Vector3.forward, Vector3.right, Vector3.right
            };
            private static readonly Vector3[] FaceUp =
            {
                Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.back, Vector3.forward
            };

            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector3> normals = new List<Vector3>();
            private readonly List<Vector2> uv = new List<Vector2>();
            private readonly List<List<int>> indices = new List<List<int>>();
            internal List<Material> Materials { get; } = new List<Material>();

            internal void Beam(Vector3 a, Vector3 b, float thickness, Material material)
            {
                Vector3 direction = b - a;
                Box((a + b) * .5f, new Vector3(thickness, thickness, direction.magnitude),
                    material, Quaternion.LookRotation(direction));
            }

            internal void Box(Vector3 center, Vector3 size, Material material, float tileWidth = 0f)
            {
                Box(center, size, material, Quaternion.identity, tileWidth);
            }

            internal void Box(Vector3 center, Vector3 size, Material material, Quaternion rotation, float tileWidth = 0f)
            {
                List<int> triangles = GetTriangles(material);
                Vector3 half = size * .5f;
                for (int i = 0; i < FaceRight.Length; i++)
                {
                    Vector3 faceRight = FaceRight[i];
                    Vector3 faceUp = FaceUp[i];
                    Vector3 normal = Vector3.Cross(faceRight, faceUp);
                    Vector3 origin = center + rotation * Vector3.Scale(normal, half);
                    Vector3 across = rotation * Vector3.Scale(faceRight, half);
                    Vector3 up = rotation * Vector3.Scale(faceUp, half);
                    Rect uvBounds = new Rect(0f, 0f, 1f, 1f);
                    if (tileWidth > 0f)
                    {
                        float u = Vector3.Dot(origin, rotation * faceRight);
                        float v = Vector3.Dot(origin, rotation * faceUp);
                        uvBounds = new Rect((u - across.magnitude) / tileWidth, (v - up.magnitude) / tileWidth,
                            2f * across.magnitude / tileWidth, 2f * up.magnitude / tileWidth);
                    }
                    AddFace(origin, across, up, rotation * normal, uvBounds, triangles);
                }
            }

            internal void Pane(Vector3 center, Vector3 across, float halfHeight, Material material, Rect uvBounds)
            {
                AddFace(center, across, Vector3.up * halfHeight, Vector3.Cross(across, Vector3.up).normalized,
                    uvBounds, GetTriangles(material));
            }

            internal void Slab(float bottom, float top, float width, float bevel, Material material, float tileWidth)
            {
                float half = width * .5f;
                float inset = half - bevel;
                float[] heights = { bottom, bottom + bevel, top - bevel, top };
                float[] radii = { inset, half, half, inset };
                List<int> triangles = GetTriangles(material);

                for (int side = 0; side < 4; side++)
                {
                    Vector3 across = FaceRight[side];
                    Vector3 outward = Vector3.Cross(across, Vector3.up);
                    for (int edge = 0; edge < heights.Length - 1; edge++)
                    {
                        float lower = radii[edge];
                        float upper = radii[edge + 1];
                        Vector3 a = Vector3.up * heights[edge] + outward * lower;
                        Vector3 b = Vector3.up * heights[edge + 1] + outward * upper;
                        Vector3 normal = Vector3.Cross(across, b - a).normalized;
                        float rise = heights[edge + 1] - heights[edge];
                        AddQuad(a - across * lower, a + across * lower, b + across * upper, b - across * upper,
                            normal, new Rect(-half / tileWidth, heights[edge] / tileWidth, width / tileWidth,
                                rise / tileWidth), triangles);
                    }
                }

                float uvInset = inset / tileWidth;
                var capUv = new Rect(-uvInset, -uvInset, uvInset * 2f, uvInset * 2f);
                AddFace(Vector3.up * top, Vector3.right * inset, Vector3.back * inset, Vector3.up, capUv, triangles);
                AddFace(Vector3.up * bottom, Vector3.right * inset, Vector3.forward * inset, Vector3.down, capUv, triangles);
            }

            private List<int> GetTriangles(Material material)
            {
                int submesh = Materials.IndexOf(material);
                if (submesh >= 0)
                    return indices[submesh];

                var triangles = new List<int>();
                Materials.Add(material);
                indices.Add(triangles);
                return triangles;
            }

            private void AddFace(Vector3 center, Vector3 across, Vector3 up, Vector3 normal, Rect uvBounds,
                List<int> triangles)
            {
                AddQuad(center - across - up, center + across - up, center + across + up, center - across + up,
                    normal, uvBounds, triangles);
            }

            private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Rect uvBounds,
                List<int> triangles)
            {
                int index = vertices.Count;
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);
                vertices.Add(d);
                uv.Add(new Vector2(uvBounds.xMin, uvBounds.yMin));
                uv.Add(new Vector2(uvBounds.xMax, uvBounds.yMin));
                uv.Add(new Vector2(uvBounds.xMax, uvBounds.yMax));
                uv.Add(new Vector2(uvBounds.xMin, uvBounds.yMax));
                for (int i = 0; i < 4; i++)
                    normals.Add(normal);
                triangles.Add(index);
                triangles.Add(index + 1);
                triangles.Add(index + 2);
                triangles.Add(index);
                triangles.Add(index + 2);
                triangles.Add(index + 3);
            }

            internal Mesh CreateMesh(string name, Vector3? geometryScale = null)
            {
                if (geometryScale.HasValue)
                {
                    Vector3 scale = geometryScale.Value;
                    Vector3 normalScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        vertices[i] = Vector3.Scale(vertices[i], scale);
                        normals[i] = Vector3.Scale(normals[i], normalScale).normalized;
                    }
                }

                var mesh = new Mesh { name = name };
                if (vertices.Count > ushort.MaxValue)
                    mesh.indexFormat = IndexFormat.UInt32;
                mesh.subMeshCount = indices.Count;
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uv);
                for (int i = 0; i < indices.Count; i++)
                    mesh.SetTriangles(indices[i], i, false);
                mesh.RecalculateBounds();
                mesh.UploadMeshData(true);
                return mesh;
            }
        }
    }
}
