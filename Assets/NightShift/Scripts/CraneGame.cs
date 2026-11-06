using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace NightShift
{
    public sealed class CraneGame : MonoBehaviour
    {
        private enum Phase { Ready, Aiming, Landing, Framing, Falling, Ended }

        private const float FloorHeight = 1.2f;
        private const float HookHeight = 8.8f;
        private const float SuspensionGap = 3.7f;
        private const string StandardBestKey = "NightShiftCrane.Best";
        private const string CasualBestKey = "NightShiftCrane.Best.Casual";
        private static readonly Vector2 FloorSize = new Vector2(5.2f, 5.2f);
        private static readonly Vector3 CameraOffset = new Vector3(15f, 20f, -22f);
        private static readonly Color CautionColor = new Color(1f, 0.72f, 0.34f);
        private static readonly Color SafeColor = new Color(0.40f, 0.91f, 0.81f);
        private static readonly Color UnsafeColor = new Color(1f, 0.49f, 0.34f);

        [SerializeField] private Shader surfaceShader;
        [SerializeField] private Shader unlitShader;
        [SerializeField] private Shader cloudShader;
        [SerializeField] private TMP_FontAsset chineseFont;
        [SerializeField] private TMP_FontAsset displayFont;
        [SerializeField] private Texture2D wetAsphalt;
        [SerializeField] private Texture2D rainConcrete;
        [SerializeField] private Texture2D windowRoom;
        [SerializeField] private Shader landingGlowShader;

        private readonly List<FloorFootprint> settled = new List<FloorFootprint>();
        private readonly List<GameObject> settledVisuals = new List<GameObject>();
        private readonly Transform[] ropes = new Transform[5];
        private NightShiftArt art;
        private NightShiftHud hud;
        private AltitudeBackdrop backdrop;
        private Camera gameplayCamera;
        private Transform runRoot;
        private Transform craneAnchor;
        private GameObject crane;
        private Transform hook;
        private GameObject suspended;
        private LineRenderer supportOutline;
        private LineRenderer supportHalo;
        private Material supportHaloMaterial;
        private LineRenderer projectedOutline;
        private LineRenderer jointOutline;
        private MaterialPropertyBlock guideColor;
        private AudioSource audioSource;
        private AudioClip impact;
        private AudioClip chime;

        private Phase phase;
        private GameMode mode = GameMode.Casual;
        private PlacementResult landing;
        private Transform collapseGroup;
        private Vector3 collapsePivot;
        private Vector3 failureFocus;
        private Vector3 cameraFocus;
        private Vector3 cameraVelocity;
        private Vector3 dropStart;
        private Vector3 dropEnd;
        private Vector3 fallDirection;
        private Quaternion dropRotation;
        private float swingTime;
        private float phaseTime;
        private int inputArmedFrame;
        private int bestScore;
        private Vector2 towerWeightedCenter;
        private float towerArea;
        private bool paused;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private NightShiftDebugPanel debugPanel;
        private bool debugRun;
        internal bool DebugFrozen { get; private set; }
        internal int DebugFloor => Score;
#endif

        private int Score => settled.Count - 1;
        private float RoofFraming => Mathf.Lerp(0.28f, 1.5f, Mathf.Clamp01(Score / 10f));
        private string BestScoreKey => mode == GameMode.Standard ? StandardBestKey : CasualBestKey;
        private Vector3 LandingPoint => AtHeight(settled[settled.Count - 1].Center, settled.Count * FloorHeight);

        private void Awake()
        {
            Application.targetFrameRate = 60;
            art = new NightShiftArt(transform, surfaceShader, unlitShader, wetAsphalt, rainConcrete, windowRoom);
            if (landingGlowShader != null)
                supportHaloMaterial = new Material(landingGlowShader) { name = "Warm roof halo" };
            backdrop = new AltitudeBackdrop(transform, art.CityRoot, cloudShader, unlitShader);
            var view = new GameObject("Tower camera");
            view.transform.SetParent(transform, false);
            view.tag = "MainCamera";
            gameplayCamera = view.AddComponent<Camera>();
            gameplayCamera.clearFlags = CameraClearFlags.SolidColor;
            gameplayCamera.nearClipPlane = 0.4f;
            gameplayCamera.farClipPlane = 135f;
            gameplayCamera.allowHDR = false;
            gameplayCamera.allowMSAA = true;
            view.AddComponent<AudioListener>();
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            impact = MakeSound("Concrete impact", 0.22f, 72f, 0.48f);
            chime = MakeSound("Perfect lock", 0.29f, 575f, 0.05f);

            hud = new NightShiftHud(transform, chineseFont, displayFont, StartRun, () => StartRun(mode), ShowModeSelection);
            ShowModeSelection();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            debugPanel = gameObject.AddComponent<NightShiftDebugPanel>();
            debugPanel.Initialize(this, chineseFont);
#endif
        }

        private void Update()
        {
            if (paused)
                return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugPanel != null && debugPanel.IsOpen)
                return;
#endif
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            float swayStep = dt;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (DebugFrozen)
                swayStep = 0f;
#endif
            bool key = Input.GetKeyDown(KeyCode.Space);
            if (key && (phase == Phase.Ready || phase == Phase.Ended) && !MenuButtonSelected())
            {
                StartRun(mode);
                return;
            }

            switch (phase)
            {
                case Phase.Ready:
                    UpdateSuspension(swayStep);
                    break;
                case Phase.Aiming:
                    // Commit the pose already displayed, so a teal preview and a tap agree.
                    if (Time.frameCount >= inputArmedFrame && (key || PressedPlayfield()))
                        ReleaseFloor();
                    else
                        UpdateSuspension(swayStep);
                    break;
                case Phase.Landing:
                    UpdateLanding(dt);
                    break;
                case Phase.Framing:
                    if (Vector3.SqrMagnitude(cameraFocus - FocusPoint()) < 0.012f)
                    {
                        SpawnFloor();
                        phase = Phase.Aiming;
                        inputArmedFrame = Time.frameCount + 1;
                    }
                    break;
                case Phase.Falling:
                    UpdateFall(dt);
                    break;
            }

            UpdateCamera(dt);
        }

        private void ShowModeSelection()
        {
            phase = Phase.Ready;
            ResetTower();
            hud.SetReady(PlayerPrefs.GetInt(CasualBestKey), PlayerPrefs.GetInt(StandardBestKey));
        }

        private void StartRun(GameMode choice)
        {
            mode = choice;
            bestScore = PlayerPrefs.GetInt(BestScoreKey);
            phase = Phase.Aiming;
            ResetTower();
            inputArmedFrame = Time.frameCount + 2;
            hud.SetPlaying(mode, 0, bestScore);
        }

        private void ResetTower()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            debugRun = false;
            DebugFrozen = false;
#endif
            if (runRoot != null)
            {
                runRoot.gameObject.SetActive(false);
                Destroy(runRoot.gameObject);
            }
            runRoot = NewChild("Current tower", transform);
            art.BeginRun(Random.Range(0, int.MaxValue));
            settled.Clear();
            settledVisuals.Clear();
            var baseFloor = new FloorFootprint(Vector2.zero, FloorSize);
            settled.Add(baseFloor);
            towerWeightedCenter = baseFloor.Center * baseFloor.Area;
            towerArea = baseFloor.Area;
            hud.SetTowerCenter(baseFloor.Center, baseFloor);
            settledVisuals.Add(art.CreateFloor(Vector3.zero, FloorSize, FloorHeight, runRoot, 0));
            art.SetFloorLit(settledVisuals[0], false);

            craneAnchor = NewChild("Crane lift rig", runRoot);
            crane = art.CreateCrane(craneAnchor);
            hook = art.CreateHook(runRoot);
            for (int i = 0; i < ropes.Length; i++)
            {
                var rope = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rope.name = i == 0 ? "Hoist cable" : "Lifting sling";
                rope.transform.SetParent(runRoot, false);
                rope.GetComponent<Renderer>().sharedMaterial = art.CableMaterial;
                Destroy(rope.GetComponent<Collider>());
                ropes[i] = rope.transform;
            }
            supportOutline = CreateOutline("Support boundary", 0.052f);
            supportHalo = supportHaloMaterial != null ? CreateOutline("Support halo", 0.20f) : null;
            if (supportHalo != null)
                supportHalo.sharedMaterial = supportHaloMaterial;
            projectedOutline = CreateOutline("Projected floor boundary", 0.023f);
            jointOutline = CreateOutline("Critical structural joint", 0.075f);
            guideColor = new MaterialPropertyBlock();
            collapseGroup = null;
            ClearJoint();
            cameraFocus = FocusPoint();
            cameraVelocity = Vector3.zero;
            phaseTime = 0f;
            SpawnFloor();
            UpdateCamera(0f);
        }

        private void SpawnFloor()
        {
            swingTime = Score % 2 == 0 ? -0.82f : 0.89f;
            suspended = art.CreateFloor(LandingPoint + Vector3.up * SuspensionGap, FloorSize, FloorHeight, runRoot,
                settled.Count);
            suspended.name = "Suspended floor";
            craneAnchor.position = LandingPoint + Vector3.up * HookHeight;
            SetRectangle(supportOutline, settled[settled.Count - 1], LandingPoint.y + 0.035f);
            if (supportHalo != null)
                SetRectangle(supportHalo, settled[settled.Count - 1], LandingPoint.y + 0.033f);
            SetRigVisible(true);
            UpdateSuspension(0f);
        }

        private void UpdateSuspension(float dt)
        {
            float difficulty = Mathf.Clamp01(Score / 22f);
            swingTime += dt * Mathf.Lerp(1.09f, 2.18f, difficulty);
            float travel = Mathf.Sin(swingTime) * Mathf.Lerp(3.04f, 4.08f, difficulty);
            float pendulum = Mathf.Sin(swingTime * 1.84f + 0.56f) * Mathf.Lerp(0.18f, 0.38f, difficulty);
            float crossSway = Mathf.Sin(swingTime * 1.27f + Score * 0.69f) * Mathf.Lerp(0.13f, 0.30f, difficulty);
            art.SetTrolley(crane, travel);
            Vector3 pivot = craneAnchor.position + Vector3.right * travel;
            Vector3 hookPosition = pivot - Vector3.up * 1.80f + Vector3.right * (pendulum * 0.55f) +
                Vector3.forward * (crossSway * 0.60f);
            hook.position = hookPosition;
            suspended.transform.SetPositionAndRotation(
                LandingPoint + Vector3.up * (SuspensionGap + Mathf.Abs(pendulum) * 0.12f) +
                Vector3.right * (travel + pendulum) + Vector3.forward * crossSway,
                Quaternion.Euler(crossSway * 10f, 0f, -pendulum * 12f));

            PlaceCable(ropes[0], pivot, hookPosition, 0.035f);
            for (int i = 0; i < 4; i++)
            {
                // Floor roots scale a unit box from its bottom center.
                var corner = new Vector3(i % 2 == 0 ? -0.42f : 0.42f, 1.02f, i < 2 ? -0.42f : 0.42f);
                PlaceCable(ropes[i + 1], hookPosition, suspended.transform.TransformPoint(corner), 0.023f);
            }

            FloorFootprint prediction = IncomingFootprint();
            PlacementResult preview = StackRules.Evaluate(settled, prediction, mode);
            SetRectangle(projectedOutline, preview.IsPerfect && preview.IsStable ? preview.Footprint : prediction,
                LandingPoint.y + 0.047f);
            SetGuideColor(projectedOutline, preview.IsStable ? SafeColor : UnsafeColor);
            if (phase == Phase.Aiming)
                ShowJoint(preview);
        }

        private void ReleaseFloor()
        {
            landing = StackRules.Evaluate(settled, IncomingFootprint(), mode);
            dropStart = suspended.transform.position;
            dropRotation = suspended.transform.rotation;
            dropEnd = AtHeight(landing.Footprint.Center, LandingPoint.y);
            SetRigVisible(false);
            ShowJoint(landing);
            phaseTime = 0f;
            phase = Phase.Landing;
        }

        private void UpdateLanding(float dt)
        {
            phaseTime += dt;
            float progress = Mathf.Clamp01(phaseTime / 0.32f);
            suspended.transform.SetPositionAndRotation(Vector3.LerpUnclamped(dropStart, dropEnd, progress * progress),
                Quaternion.Slerp(dropRotation, Quaternion.identity, progress));
            if (progress < 1f)
                return;

            if (!landing.IsStable)
            {
                BeginFailure();
                return;
            }

            ClearJoint();
            AddSettledFloor(landing.Footprint, suspended, landing.IsPerfect || settled.Count % 3 == 0);
            suspended = null;
            RecordBest();
            hud.SetScore(Score, bestScore);
            audioSource.PlayOneShot(impact, 0.65f);
            if (landing.IsPerfect)
                audioSource.PlayOneShot(chime, 0.45f);
            phase = Phase.Framing;
            SetSupportVisible(false);
        }

        private void AddSettledFloor(FloorFootprint footprint, GameObject visual, bool lit)
        {
            settled.Add(footprint);
            towerWeightedCenter += footprint.Center * footprint.Area;
            towerArea += footprint.Area;
            hud.SetTowerCenter(towerWeightedCenter / towerArea, settled[0]);
            settledVisuals.Add(visual);
            visual.name = "Floor " + Score;
            art.SetFloorLit(visual, lit);
            if (settledVisuals.Count > 26)
                settledVisuals[settledVisuals.Count - 27].SetActive(false);
        }

        private void RecordBest()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugRun)
                return;
#endif
            if (Score <= bestScore)
                return;
            bestScore = Score;
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal void DebugJumpTo(int floor) => PrepareDebugStack(mode, Mathf.Clamp(floor, 0, 80), 0f);

        internal void DebugSetFrozen(bool frozen)
        {
            DebugFrozen = frozen;
            if (frozen)
                debugRun = true;
        }

        internal void DebugPerfectDrop()
        {
            if (phase == Phase.Ready || phase == Phase.Ended)
                PrepareDebugStack(mode, 0, 0f);
            if (phase != Phase.Aiming)
                return;

            debugRun = true;
            PositionDebugDrop(settled[settled.Count - 1].Center);
        }

        internal void DebugTestCollapse()
        {
            const float offset = 1.4f;
            PrepareDebugStack(GameMode.Standard, 2, offset);
            PositionDebugDrop(new Vector2(offset * 3f, 0f));
        }

        internal void DebugResetRun() => StartRun(mode);

        private void PrepareDebugStack(GameMode choice, int floors, float offsetPerFloor)
        {
            StartRun(choice);
            debugRun = true;
            if (floors == 0)
                return;

            suspended.SetActive(false);
            Destroy(suspended);
            for (int level = 1; level <= floors; level++)
            {
                var center = new Vector2(level * offsetPerFloor, 0f);
                var footprint = new FloorFootprint(center, FloorSize);
                var floor = art.CreateFloor(AtHeight(center, level * FloorHeight), FloorSize, FloorHeight, runRoot, level);
                AddSettledFloor(footprint, floor, level % 3 == 0);
            }

            cameraFocus = FocusPoint();
            cameraVelocity = Vector3.zero;
            SpawnFloor();
            UpdateCamera(0f);
            hud.SetScore(Score, bestScore);
        }

        private void PositionDebugDrop(Vector2 center)
        {
            suspended.transform.SetPositionAndRotation(AtHeight(center, suspended.transform.position.y), Quaternion.identity);
            ReleaseFloor();
        }
#endif

        private void ShowJoint(PlacementResult result)
        {
            if (mode != GameMode.Standard || !result.Joint.HasValue ||
                (result.IsStable && result.Joint.Value.Margin > 0.32f))
            {
                ClearJoint();
                return;
            }

            SupportJoint joint = result.Joint.Value;
            FloorFootprint lower = settled[joint.LowerIndex];
            SetRectangle(jointOutline, new FloorFootprint(lower.Center, lower.Size + Vector2.one * 0.14f),
                (joint.LowerIndex + 1) * FloorHeight - 0.055f);
            jointOutline.enabled = true;
            SetGuideColor(jointOutline, result.IsStable ? CautionColor : UnsafeColor);
            string message = result.IsStable
                ? JointName(joint.LowerIndex) + " · " + Local("接近边缘", "NEAR EDGE")
                : FailureLabel(result);
            hud.SetStability(joint, message, !result.IsStable);
        }

        private void ClearJoint()
        {
            jointOutline.enabled = false;
            hud.SetStability(null, string.Empty, false);
        }

        private void BeginFailure()
        {
            var delta = landing.Footprint.Center - settled[settled.Count - 1].Center;
            fallDirection = delta.sqrMagnitude < 0.0025f ? Vector3.right : AtHeight(delta.normalized, 0f);
            phaseTime = 0f;
            phase = Phase.Falling;
            audioSource.PlayOneShot(impact, 0.48f);
            if (mode != GameMode.Standard || landing.Failure != PlacementFailure.Unbalanced ||
                !landing.Joint.HasValue || landing.Joint.Value.LowerIndex >= settled.Count - 1)
                return;

            SupportJoint joint = landing.Joint.Value;
            Vector2 edge = FindTipEdge(joint, out Vector2 outward);
            float seam = (joint.LowerIndex + 1) * FloorHeight;
            fallDirection = AtHeight(outward, 0f);
            collapsePivot = AtHeight(edge, seam);
            failureFocus = AtHeight(settled[joint.LowerIndex].Center, seam + 1.8f) + fallDirection * 2.6f;
            collapseGroup = NewChild("Failed tower segment", runRoot);
            collapseGroup.position = collapsePivot;
            for (int i = Mathf.Max(0, joint.LowerIndex - 4); i < settledVisuals.Count; i++)
            {
                if (i <= joint.LowerIndex + 12)
                    settledVisuals[i].SetActive(true);
                if (i > joint.LowerIndex)
                    settledVisuals[i].transform.SetParent(collapseGroup, true);
            }
            suspended.transform.SetParent(collapseGroup, true);
            SetSupportVisible(false);
        }

        private static Vector2 FindTipEdge(SupportJoint joint, out Vector2 outward)
        {
            Vector2 point = Vector2.Max(joint.ContactMin, Vector2.Min(joint.ContactMax, joint.LoadCenter));
            Vector2 excess = joint.LoadCenter - point;
            if (excess.sqrMagnitude > 0.000001f)
            {
                outward = excess.normalized;
                return point;
            }

            // The safety inset can fail while the load is still on the physical contact.
            float left = point.x - joint.ContactMin.x;
            float right = joint.ContactMax.x - point.x;
            float down = point.y - joint.ContactMin.y;
            float up = joint.ContactMax.y - point.y;
            float nearest = Mathf.Min(Mathf.Min(left, right), Mathf.Min(down, up));
            if (nearest == left) { point.x = joint.ContactMin.x; outward = Vector2.left; }
            else if (nearest == right) { point.x = joint.ContactMax.x; outward = Vector2.right; }
            else if (nearest == down) { point.y = joint.ContactMin.y; outward = Vector2.down; }
            else { point.y = joint.ContactMax.y; outward = Vector2.up; }
            return point;
        }

        private void UpdateFall(float dt)
        {
            phaseTime += dt;
            Vector3 tipAxis = Vector3.Cross(Vector3.up, fallDirection);
            if (collapseGroup != null)
            {
                float tipTime = Mathf.Max(0f, phaseTime - 0.30f);
                float drop = Mathf.Max(0f, tipTime - 0.48f);
                collapseGroup.SetPositionAndRotation(
                    collapsePivot + fallDirection * (drop * 2f) - Vector3.up * (4.9f * drop * drop),
                    Quaternion.AngleAxis(Mathf.Min(89f, 96f * tipTime * tipTime), tipAxis));
                if (phaseTime < 1.43f)
                    return;
            }
            else
            {
                suspended.transform.SetPositionAndRotation(
                    dropEnd + fallDirection * (phaseTime * 2f) - Vector3.up * (4.9f * phaseTime * phaseTime),
                    Quaternion.AngleAxis(phaseTime * 116f, tipAxis));
                if (phaseTime < 0.64f)
                    return;
                suspended.SetActive(false);
            }
            phase = Phase.Ended;
            hud.SetEnded(Score, bestScore, FailureLabel(landing));
            PlayerPrefs.Save();
        }

        private void UpdateCamera(float dt)
        {
            if (dt > 0f)
            {
                bool showingFailure = collapseGroup != null;
                cameraFocus = Vector3.SmoothDamp(cameraFocus, showingFailure ? failureFocus : FocusPoint(),
                    ref cameraVelocity, showingFailure ? 0.25f : 0.14f, showingFailure ? 120f : 14f, dt);
            }
            if (phase == Phase.Framing)
                craneAnchor.position = cameraFocus + Vector3.up * (HookHeight + RoofFraming);
            gameplayCamera.transform.position = cameraFocus + CameraOffset;
            gameplayCamera.transform.LookAt(cameraFocus);
            float ratio = Mathf.Clamp(gameplayCamera.aspect, 0.42f, 0.86f);
            float collapseWidth = collapseGroup == null ? 0f : Mathf.SmoothStep(0f, 4f, phaseTime / 0.42f);
            float viewHeight = (13.8f + collapseWidth) / ratio;
            gameplayCamera.fieldOfView = 2f * Mathf.Atan(viewHeight / (2f * CameraOffset.magnitude)) * Mathf.Rad2Deg;
            backdrop.Apply(gameplayCamera);
        }

        private Vector3 FocusPoint() => LandingPoint - Vector3.up * RoofFraming;

        private bool PressedPlayfield()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                return touch.phase == TouchPhase.Began && IsPlayfieldPointer(touch.position, touch.fingerId);
            }
            return Input.GetMouseButtonDown(0) && IsPlayfieldPointer(Input.mousePosition, -1);
        }

        private bool IsPlayfieldPointer(Vector2 position, int pointerId)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugPanel != null && debugPanel.ContainsTrigger(position))
                return false;
#endif
            return EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(pointerId);
        }

        private static bool MenuButtonSelected()
        {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return selected != null && selected.activeInHierarchy &&
                selected.TryGetComponent(out Button button) && button.IsInteractable();
        }

        private FloorFootprint IncomingFootprint()
        {
            Vector3 position = suspended.transform.position;
            return new FloorFootprint(new Vector2(position.x, position.z), FloorSize);
        }

        private void SetRigVisible(bool visible)
        {
            foreach (Transform rope in ropes)
                rope.gameObject.SetActive(visible);
            hook.gameObject.SetActive(visible);
            projectedOutline.enabled = visible;
            SetSupportVisible(true);
        }

        private void SetSupportVisible(bool visible)
        {
            supportOutline.enabled = visible;
            if (supportHalo != null)
                supportHalo.enabled = visible;
        }

        private LineRenderer CreateOutline(string label, float width)
        {
            var line = NewChild(label, runRoot).gameObject.AddComponent<LineRenderer>();
            line.sharedMaterial = art.GuideMaterial;
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = 4;
            line.widthMultiplier = width;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private void SetGuideColor(LineRenderer line, Color color)
        {
            guideColor.SetColor("_Color", color);
            line.SetPropertyBlock(guideColor);
        }

        private static void SetRectangle(LineRenderer line, FloorFootprint area, float y)
        {
            Vector2 half = area.Size * 0.5f;
            line.SetPosition(0, AtHeight(area.Center + new Vector2(-half.x, -half.y), y));
            line.SetPosition(1, AtHeight(area.Center + new Vector2( half.x, -half.y), y));
            line.SetPosition(2, AtHeight(area.Center + new Vector2( half.x,  half.y), y));
            line.SetPosition(3, AtHeight(area.Center + new Vector2(-half.x,  half.y), y));
        }

        private static void PlaceCable(Transform cable, Vector3 from, Vector3 to, float diameter)
        {
            Vector3 delta = to - from;
            cable.SetPositionAndRotation((from + to) * 0.5f, Quaternion.FromToRotation(Vector3.up, delta));
            cable.localScale = new Vector3(diameter, delta.magnitude * 0.5f, diameter);
        }

        private static Transform NewChild(string label, Transform parent)
        {
            var child = new GameObject(label).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static Vector3 AtHeight(Vector2 point, float y) => new Vector3(point.x, y, point.y);

        private string Local(string chinese, string english) => chineseFont != null ? chinese : english;

        private string JointName(int lowerIndex) => lowerIndex == 0
            ? Local("地基接缝", "BASE JOINT")
            : Local($"第 {lowerIndex} 层接缝", $"JOINT {lowerIndex}");

        private string FailureLabel(PlacementResult result)
        {
            if (mode == GameMode.Standard && result.Failure == PlacementFailure.Unbalanced &&
                result.Joint.HasValue && result.Joint.Value.LowerIndex < settled.Count - 1)
            {
                int joint = result.Joint.Value.LowerIndex;
                return joint == 0
                    ? Local("地基失稳", "BASE FAILED")
                    : Local($"第 {joint} 层失稳", $"JOINT {joint} FAILED");
            }
            return ExplainFailure(result.Failure);
        }

        private string ExplainFailure(PlacementFailure failure)
        {
            switch (failure)
            {
                case PlacementFailure.NoOverlap: return Local("完全错开", "NO CONTACT");
                case PlacementFailure.TooLittleSupport: return Local("接触面积不足", "TOO LITTLE CONTACT");
                default: return Local("重心越界", "LOAD OUTSIDE SUPPORT");
            }
        }

        private void OnApplicationPause(bool pause)
        {
            paused = pause;
            inputArmedFrame = Time.frameCount + 2;
            if (pause)
                PlayerPrefs.Save();
        }

        private void OnDestroy()
        {
            backdrop?.Dispose();
            art?.Dispose();
            if (supportHaloMaterial != null)
                Destroy(supportHaloMaterial);
            if (impact != null)
                Destroy(impact);
            if (chime != null)
                Destroy(chime);
        }

        private static AudioClip MakeSound(string label, float duration, float pitch, float noiseAmount)
        {
            const int rate = 22050;
            var samples = new float[Mathf.RoundToInt(duration * rate)];
            var random = new System.Random(212);
            for (int i = 0; i < samples.Length; i++)
            {
                float time = (float)i / rate;
                float envelope = Mathf.Min(1f, time * 1250f) * Mathf.Exp(-time * (noiseAmount > 0.2f ? 24f : 13f));
                float tone = Mathf.Sin(time * pitch * Mathf.PI * 2f);
                float noise = (float)(random.NextDouble() * 2.0 - 1.0) * noiseAmount;
                samples[i] = (tone * (1f - noiseAmount) + noise) * envelope * 0.46f;
            }
            var clip = AudioClip.Create(label, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
