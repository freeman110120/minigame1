using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NightShift
{
    internal sealed class RoomLifeLibrary : IDisposable
    {
        internal const int ActivityCount = 8;
        private const int FrameCount = 64;
        private static readonly string[] Names =
            { "Reading", "Cooking", "Laptop", "Television", "Plants", "Stretching", "Laundry", "Conversation" };
        private readonly Mesh[][] poses = new Mesh[ActivityCount][];
        private readonly Mesh[] furniture = new Mesh[ActivityCount];
        private readonly Material resident;
        private readonly Material furnishing;
        private readonly Material screen;

        internal RoomLifeLibrary(Shader shader)
        {
            resident = new Material(shader) { name = "Residents in silhouette", color = new Color(.10f, .13f, .15f) };
            furnishing = new Material(shader) { name = "Apartment furnishings", color = new Color(.16f, .18f, .18f) };
            screen = new Material(shader) { name = "Apartment screen glow", color = new Color(.40f, .61f, .69f) };
            for (int activity = 0; activity < ActivityCount; activity++)
            {
                furniture[activity] = MakeFurniture(activity).ToMesh(Names[activity] + " furniture");
                poses[activity] = new Mesh[FrameCount];
                for (int frame = 0; frame < FrameCount; frame++)
                    poses[activity][frame] = MakeResidents(activity, frame / (float)FrameCount)
                        .ToMesh(Names[activity] + " pose " + frame);
            }
        }

        internal GameObject Create(Transform parent, int activity, int variation, float phase)
        {
            activity = Mathf.Clamp(activity, 0, ActivityCount - 1);
            var room = new GameObject(Names[activity] + " household");
            room.transform.SetParent(parent, false);
            room.transform.localScale = new Vector3((variation & 1) == 0 ? 1f : -1f, 1f, 1f);
            CreateModel("Furniture", room.transform, furniture[activity], new[] { furnishing, screen });
            MeshRenderer actor = CreateModel("Residents", room.transform, poses[activity][0], new[] { resident });
            var color = new MaterialPropertyBlock();
            color.SetColor("_Color", Color.Lerp(new Color(.075f, .10f, .12f), new Color(.18f, .16f, .15f),
                (variation % 5) / 4f));
            actor.SetPropertyBlock(color);
            room.AddComponent<NightShiftRoomLife>().Initialize(actor, poses[activity], activity, variation, phase);
            return room;
        }

        public void Dispose()
        {
            foreach (var frames in poses)
                foreach (var mesh in frames)
                    Object.Destroy(mesh);
            foreach (var mesh in furniture)
                Object.Destroy(mesh);
            Object.Destroy(resident);
            Object.Destroy(furnishing);
            Object.Destroy(screen);
        }

        private static MeshRenderer CreateModel(string name, Transform parent, Mesh mesh, Material[] materials)
        {
            var model = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            model.transform.SetParent(parent, false);
            model.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = model.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return renderer;
        }

        private static RoomMesh MakeFurniture(int activity)
        {
            var mesh = new RoomMesh(.006f, 2);
            switch (activity)
            {
                case 0:
                    Chair(mesh, -.19f, .18f, .18f);
                    mesh.Box(.22f, .15f, .18f, .025f);
                    mesh.Stroke(new Vector2(.22f, .15f), new Vector2(.22f, .02f), .018f);
                    mesh.Stroke(new Vector2(.28f, .15f), new Vector2(.28f, .43f), .012f);
                    mesh.Polygon(new[] { V(.21f, .42f), V(.34f, .42f), V(.31f, .48f), V(.24f, .48f) });
                    mesh.Box(-.27f, .56f, .16f, .023f);
                    Books(mesh, -.32f, .59f);
                    break;
                case 1:
                    Counter(mesh, .19f, .265f);
                    mesh.Box(.21f, .32f, .105f, .065f);
                    mesh.Box(.275f, .34f, .06f, .014f);
                    mesh.Box(.20f, .535f, .29f, .095f);
                    mesh.Box(.20f, .48f, .31f, .015f);
                    mesh.Box(.08f, .43f, .03f, .035f);
                    mesh.Stroke(V(.08f, .45f), V(.08f, .49f), .008f);
                    break;
                case 2:
                    Chair(mesh, -.22f, .17f, .15f);
                    Table(mesh, .13f, .255f, .40f);
                    mesh.Box(.17f, .28f, .18f, .015f);
                    mesh.Polygon(new[] { V(.19f, .28f), V(.25f, .28f), V(.28f, .39f), V(.23f, .39f) });
                    mesh.Box(.253f, .339f, .022f, .071f, 1);
                    mesh.Box(.33f, .29f, .04f, .05f);
                    Books(mesh, .09f, .56f);
                    mesh.Box(.20f, .52f, .28f, .02f);
                    break;
                case 3:
                    Chair(mesh, -.17f, .16f, .27f);
                    mesh.Box(.24f, .30f, .22f, .20f);
                    mesh.Box(.24f, .307f, .19f, .16f, 1);
                    mesh.Stroke(V(.24f, .20f), V(.24f, .155f), .02f);
                    Table(mesh, .24f, .14f, .25f);
                    mesh.Box(-.29f, .46f, .13f, .09f);
                    break;
                case 4:
                    Table(mesh, .23f, .16f, .26f);
                    Plant(mesh, .20f, .18f, .95f);
                    Plant(mesh, .32f, .18f, .58f);
                    mesh.Box(-.25f, .55f, .17f, .016f);
                    mesh.Box(-.24f, .58f, .06f, .045f);
                    break;
                case 5:
                    mesh.Box(0f, .024f, .63f, .02f);
                    mesh.Box(-.29f, .075f, .055f, .10f);
                    mesh.Box(-.29f, .132f, .025f, .016f);
                    Plant(mesh, .30f, .035f, .75f);
                    mesh.Box(-.22f, .51f, .13f, .09f);
                    break;
                case 6:
                    Table(mesh, .18f, .245f, .38f);
                    mesh.Box(.30f, .27f, .075f, .024f);
                    mesh.Box(.29f, .29f, .077f, .016f);
                    mesh.Box(.29f, .307f, .065f, .016f);
                    mesh.Polygon(new[] { V(.07f, .03f), V(.29f, .03f), V(.32f, .13f), V(.04f, .13f) });
                    mesh.Stroke(V(.055f, .13f), V(.31f, .13f), .014f);
                    mesh.Stroke(V(-.33f, .52f), V(.34f, .52f), .008f);
                    mesh.Box(.19f, .45f, .11f, .12f);
                    break;
                case 7:
                    Table(mesh, 0f, .185f, .23f);
                    mesh.Box(-.045f, .22f, .035f, .047f);
                    mesh.Box(.055f, .22f, .035f, .047f);
                    Plant(mesh, -.33f, .03f, .60f);
                    mesh.Box(.21f, .56f, .19f, .02f);
                    Books(mesh, .16f, .59f);
                    break;
            }
            return mesh;
        }

        private static RoomMesh MakeResidents(int activity, float time)
        {
            var mesh = new RoomMesh(.018f, 1);
            float action = Pulse(time, .20f, .65f);
            float second = Pulse(time, .72f, .86f);
            var body = new Person(mesh);
            switch (activity)
            {
                case 0:
                    body.Seated(-.17f, .21f, -.018f, action * .008f);
                    body.Arms(V(-.08f, .29f), V(.035f, .32f), V(-.05f, .31f), V(.04f, .34f + .035f * second));
                    mesh.Polygon(new[] { V(-.005f, .32f), V(.065f, .31f), V(.10f, .355f), V(.03f, .365f) });
                    mesh.Stroke(V(.037f, .33f), V(.065f, .357f), .005f);
                    break;
                case 1:
                    body.Standing(-.16f, .005f, .025f * action);
                    body.Arms(V(-.045f, .33f), V(.15f + .025f * Mathf.Sin(time * 36f) * action, .36f),
                        V(-.06f, .30f), V(.12f, .30f));
                    mesh.Stroke(V(.15f + .025f * Mathf.Sin(time * 36f) * action, .36f), V(.22f, .33f), .008f);
                    break;
                case 2:
                    body.Seated(-.19f, .19f, .012f + .022f * action, second * .014f);
                    body.Arms(V(-.05f, .265f), V(.095f, .284f + .008f * Mathf.Sin(time * 85f) * action),
                        V(-.055f, .29f), V(.065f, .286f - .008f * Mathf.Sin(time * 85f) * action));
                    break;
                case 3:
                    body.Seated(-.18f, .19f, -.035f, 0f);
                    Vector2 cup = Vector2.Lerp(V(-.02f, .235f), V(-.12f, .37f), second);
                    body.Arms(V(-.06f, .235f), cup, V(-.15f, .24f), V(-.04f, .23f));
                    mesh.Box(cup.x + .018f, cup.y + .013f, .035f, .037f);
                    break;
                case 4:
                    body.Standing(-.16f, .005f, .032f * action);
                    Vector2 can = Vector2.Lerp(V(-.025f, .29f), V(.09f, .40f), action);
                    body.Arms(V(-.025f, .345f), can, V(-.16f, .28f), V(-.08f, .26f));
                    mesh.Box(can.x + .025f, can.y - .015f, .066f, .049f);
                    mesh.Stroke(can + V(.05f, -.015f), can + V(.105f, -.045f * action), .016f);
                    if (action > .72f)
                        mesh.Stroke(can + V(.105f, -.045f), V(.20f, .31f), .004f);
                    break;
                case 5:
                    body.Standing(0f, .035f, -.015f * action);
                    body.Arms(Vector2.Lerp(V(-.07f, .33f), V(-.12f, .47f), action),
                        Vector2.Lerp(V(-.085f, .25f), V(-.04f, .595f), action),
                        Vector2.Lerp(V(.07f, .33f), V(.12f, .47f), action),
                        Vector2.Lerp(V(.085f, .25f), V(.04f, .595f), action));
                    break;
                case 6:
                    body.Standing(-.16f, .005f, .04f * action);
                    Vector2 left = V(.03f + action * .035f, .29f + .09f * action);
                    Vector2 right = V(.19f - action * .07f, .29f + .10f * action);
                    body.Arms(V(-.06f, .32f), left, V(-.03f, .33f), right);
                    mesh.Polygon(new[] { left, right, right + V(0f, -.065f), left + V(0f, -.065f) });
                    break;
                case 7:
                    body.Standing(-.22f, .025f, .008f * second, .88f);
                    body.Arms(V(-.13f, .30f), V(-.055f, .29f + .09f * action), V(-.21f, .255f), V(-.17f, .21f));
                    var partner = new Person(mesh, -1f);
                    partner.Standing(.23f, .025f, -.01f * action, .91f);
                    partner.Arms(V(.14f, .29f), V(.075f, .31f + .04f * second), V(.22f, .25f), V(.18f, .20f));
                    break;
            }
            return mesh;
        }

        // Smooth rise and return leave quiet intervals between each complete action.
        private static float Pulse(float time, float begin, float end)
        {
            float position = Mathf.Clamp01((time - begin) / (end - begin));
            return Mathf.SmoothStep(0f, 1f, Mathf.Sin(position * Mathf.PI));
        }

        private static Vector2 V(float x, float y) => new Vector2(x, y);

        private static void Chair(RoomMesh mesh, float x, float y, float width)
        {
            mesh.Box(x, y, width, .045f);
            mesh.Box(x - width * .42f, y + .08f, .035f, .18f);
            mesh.Stroke(V(x - width * .3f, y), V(x - width * .3f, .025f), .018f);
            mesh.Stroke(V(x + width * .3f, y), V(x + width * .3f, .025f), .018f);
        }

        private static void Table(RoomMesh mesh, float x, float y, float width)
        {
            mesh.Box(x, y, width, .024f);
            mesh.Stroke(V(x - width * .39f, y), V(x - width * .39f, .025f), .019f);
            mesh.Stroke(V(x + width * .39f, y), V(x + width * .39f, .025f), .019f);
        }

        private static void Counter(RoomMesh mesh, float x, float y)
        {
            mesh.Box(x, y * .5f, .32f, y);
            mesh.Box(x, y + .01f, .35f, .028f);
            mesh.Box(x, y * .5f, .006f, y * .83f, 1);
        }

        private static void Plant(RoomMesh mesh, float x, float y, float size)
        {
            mesh.Polygon(new[] { V(x - .04f * size, y), V(x + .04f * size, y),
                V(x + .055f * size, y + .075f * size), V(x - .055f * size, y + .075f * size) });
            mesh.Stroke(V(x, y + .05f * size), V(x, y + .27f * size), .009f * size);
            for (int i = 0; i < 3; i++)
            {
                float height = y + (.12f + .055f * i) * size;
                mesh.Oval(V(x - .026f * size, height), V(.046f * size, .016f * size), -24f);
                mesh.Oval(V(x + .026f * size, height + .024f * size), V(.046f * size, .016f * size), 24f);
            }
        }

        private static void Books(RoomMesh mesh, float x, float y)
        {
            for (int book = 0; book < 4; book++)
                mesh.Box(x + book * .026f, y, .018f, .055f + (book % 2) * .014f);
        }

        private sealed class Person
        {
            private readonly RoomMesh mesh;
            private readonly float direction;
            private Vector2 shoulder;
            internal Person(RoomMesh mesh, float direction = 1f) { this.mesh = mesh; this.direction = direction; }

            internal void Standing(float x, float y, float lean, float scale = 1f)
            {
                Vector2 hip = V(x, y + .24f * scale);
                shoulder = V(x + lean, y + .405f * scale);
                Torso(hip, shoulder, scale);
                Limb(hip + V(-.018f, 0f), V(x - .026f, y + .12f * scale), V(x - .033f, y + .025f), .034f * scale);
                Limb(hip + V(.018f, 0f), V(x + .04f, y + .12f * scale), V(x + .048f, y + .025f), .032f * scale);
            }

            internal void Seated(float x, float y, float lean, float nod)
            {
                Vector2 hip = V(x, y);
                shoulder = V(x + lean, y + .165f - nod);
                Torso(hip, shoulder, 1f);
                Limb(hip, V(x + .12f, y - .015f), V(x + .14f, .035f), .034f);
                Limb(hip + V(.015f, -.013f), V(x + .095f, y - .04f), V(x + .10f, .025f), .032f);
            }

            private void Torso(Vector2 hip, Vector2 shoulders, float scale)
            {
                mesh.Polygon(new[] { hip + V(-.038f * scale, -.015f), hip + V(.041f * scale, -.015f),
                    shoulders + V(.049f * scale, .005f), shoulders + V(.027f * scale, .018f),
                    shoulders + V(-.034f * scale, .018f), shoulders + V(-.048f * scale, -.009f) });
                Vector2 head = shoulders + V(.009f * direction * scale, .063f * scale);
                mesh.Stroke(shoulders, head, .030f * scale);
                mesh.Oval(head, V(.035f * scale, .046f * scale), -8f * direction);
                mesh.Oval(head + V(.031f * direction * scale, -.003f), V(.012f * scale, .012f * scale));
            }

            internal void Arms(Vector2 elbow, Vector2 hand, Vector2 secondElbow, Vector2 secondHand)
            {
                Limb(shoulder + V(-.023f, -.005f), secondElbow, secondHand, .022f);
                Limb(shoulder + V(.023f, -.008f), elbow, hand, .024f);
            }

            private void Limb(Vector2 start, Vector2 joint, Vector2 end, float width)
            {
                mesh.Stroke(start, joint, width);
                mesh.Stroke(joint, end, width * .82f);
                mesh.Oval(joint, V(width * .5f, width * .5f));
                mesh.Oval(end, V(width * .45f, width * .5f));
            }
        }

        private sealed class RoomMesh
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<int>[] triangles;
            private readonly float depth;
            internal RoomMesh(float depth, int materials)
            {
                this.depth = depth;
                triangles = new List<int>[materials];
                for (int i = 0; i < materials; i++) triangles[i] = new List<int>();
            }

            internal void Box(float x, float y, float width, float height, int material = 0)
            {
                Polygon(new[] { V(x - width * .5f, y - height * .5f), V(x + width * .5f, y - height * .5f),
                    V(x + width * .5f, y + height * .5f), V(x - width * .5f, y + height * .5f) }, material);
            }

            internal void Stroke(Vector2 from, Vector2 to, float width)
            {
                Vector2 direction = (to - from).normalized;
                Vector2 edge = new Vector2(-direction.y, direction.x) * width * .5f;
                Polygon(new[] { from - edge, to - edge, to + edge, from + edge });
            }

            internal void Oval(Vector2 center, Vector2 radius, float angle = 0f)
            {
                var points = new Vector2[12];
                Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
                for (int i = 0; i < points.Length; i++)
                {
                    float theta = i * Mathf.PI * 2f / points.Length;
                    points[i] = center + (Vector2)(rotation * new Vector3(Mathf.Cos(theta) * radius.x,
                        Mathf.Sin(theta) * radius.y, 0f));
                }
                Polygon(points);
            }

            internal void Polygon(Vector2[] points, int material = 0)
            {
                int start = vertices.Count;
                foreach (Vector2 point in points) vertices.Add(new Vector3(point.x, point.y, depth));
                for (int i = 1; i < points.Length - 1; i++)
                {
                    triangles[material].Add(start);
                    triangles[material].Add(start + i);
                    triangles[material].Add(start + i + 1);
                    // Both sides remain visible when a household is mirrored.
                    triangles[material].Add(start);
                    triangles[material].Add(start + i + 1);
                    triangles[material].Add(start + i);
                }
            }

            internal Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name, subMeshCount = triangles.Length };
                mesh.SetVertices(vertices);
                for (int i = 0; i < triangles.Length; i++) mesh.SetTriangles(triangles[i], i, false);
                mesh.RecalculateBounds();
                mesh.UploadMeshData(true);
                return mesh;
            }
        }
    }

    internal sealed class NightShiftRoomLife : MonoBehaviour
    {
        private MeshRenderer resident;
        private MeshFilter meshFilter;
        private Mesh[] poses;
        private float duration;
        private float phase;
        internal int Activity { get; private set; }

        internal void Initialize(MeshRenderer renderer, Mesh[] frames, int activity, int variation, float offset)
        {
            resident = renderer;
            meshFilter = renderer.GetComponent<MeshFilter>();
            poses = frames;
            Activity = activity;
            duration = 10f + Mathf.Abs(variation % 7) * 1.7f;
            phase = offset;
            SetPose(Time.time);
        }

        private void LateUpdate()
        {
            if (resident.isVisible)
                SetPose(Time.time);
        }

        private void SetPose(float now)
        {
            int frame = Mathf.FloorToInt(Mathf.Repeat((now + phase) / duration, 1f) * poses.Length);
            if (meshFilter.sharedMesh != poses[frame])
                meshFilter.sharedMesh = poses[frame];
        }
    }
}
