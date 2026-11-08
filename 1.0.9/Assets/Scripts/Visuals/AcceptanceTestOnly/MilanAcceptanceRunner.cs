#if UNITY_EDITOR || MILAN_ACCEPTANCE_DIAGNOSTICS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace MilanAcceptance
{
    [Serializable] public sealed class RunConfig
    {
        public string scenePath;
        public string outputDirectory;
        public string viewModelRootPath;
        public bool setInitialPosition;
        public Vector3 initialPosition;
        public float initialYaw;
        public float mouseSensitivity = 0.08f;
        public int repetitions = 1;
        public bool stopOnFailure = true;
        public bool nearWallFixture;
        public Step[] steps;
    }
    [Serializable] public sealed class Step
    {
        public string id;
        public string[] keys = new string[0];
        public float seconds = 1;
        public Vector2 mousePixelsPerSecond;
        public bool leftClick;
        public bool waypoint;
        public Vector3 target;
        public float horizontalTolerance = 0.2f;
        public float verticalTolerance = 0.35f;
        public string movementKey = "W";
        public float turnDegreesPerSecond = 150;
        public bool setPitch;
        public float targetPitch;
        public float minimumTravel;
        public float maximumTravel = -1;
        public bool screenshot = true;
    }
    [Serializable] public sealed class Sample
    {
        public int frame, repetition, step;
        public string id, cursor, moveDevice, lookDevice, handClip;
        public float time, deltaTime, unscaledDeltaTime, playerYaw, eyeHeight, fov, capsuleHeight, actualSpeed, wallDistance;
        public Vector3 position, cameraPosition, cameraEuler, displacement;
        public Vector2 commandedMouse, actionMove, actionLook;
        public string[] commandedKeys;
        public bool grounded, controllerGrounded, actionJump, actionSprint, actionCrouch;
        public float eyeAboveGround;
        public uint jumpSequence, landSequence;
        public int handStateHash;
        public string[] touchedColliders;
    }
    [Serializable] public sealed class StepResult
    {
        public string id, outcome;
        public int repetition, frames;
        public Vector3 start, end;
        public float elapsed, travelled, maxObservedSpeed;
    }
    [Serializable] public sealed class Summary
    {
        public string status = "recorded_requires_review";
        public string testMode = "Unity Play Mode, synthetic Input System devices; not manual keyboard/mouse playtest";
        public string visualAssessment = "pending; screenshots do not automatically establish absence of hand/wall clipping";
        public string scene;
        public int errors, exceptions, failedSteps, missingScripts, expectedSteps;
        public bool complete;
        public List<StepResult> steps = new List<StepResult>();
    }
    [Serializable] public sealed class MotionEvidence
    {
        public string path;
        public Vector3 initialPosition, initialScale, currentPosition, currentScale;
        public Quaternion initialRotation, currentRotation;
        public bool observedChange;
    }
    [Serializable] public sealed class VisualEvidence
    {
        public string viewModelStatus;
        public float wallRayDistance, viewModelFurthestForward;
        public string geometryAssessment = "bounds_and_center_ray_only_requires_rendered_review";
        public List<MotionEvidence> environmentalMotions = new List<MotionEvidence>();
    }
    // LateUpdate observes the real controller after its Update; it never calls Move or its update methods.
    [DefaultExecutionOrder(32000)]
    public sealed class MilanAcceptanceRunner : MonoBehaviour
    {
        public static Action<int> Completed;
        RunConfig config;
        MonoBehaviour player;
        CharacterController capsule;
        Camera eye;
        InputActionMap actions;
        Keyboard keyboard;
        Mouse mouse;
        StreamWriter samples, events;
        readonly Summary summary = new Summary();
        readonly List<string> contacts = new List<string>();
        readonly List<Transform> motionTransforms = new List<Transform>();
        readonly VisualEvidence visual = new VisualEvidence();
        int index, repetition, frames;
        float elapsed, travelled, maxSpeed;
        Vector3 lastPosition, stepStart;
        Key[] commandedKeys = new Key[0];
        Vector2 commandedMouse;
        bool initialized, done;
        Step Current => config.steps[index];

        public void Initialize(RunConfig value)
        {
            config = value;
            if (config.steps == null || config.steps.Length == 0 || config.repetitions < 1 || config.mouseSensitivity <= 0)
                throw new ArgumentException("A nonempty, reviewed route with valid sensitivity is required.");
            foreach (Step step in config.steps)
                if (step.seconds <= 0 || string.IsNullOrEmpty(step.id)) throw new ArgumentException("Invalid step.");
            Directory.CreateDirectory(config.outputDirectory);
            samples = new StreamWriter(Path.Combine(config.outputDirectory, "trajectory.jsonl"), false) { AutoFlush = true };
            events = new StreamWriter(Path.Combine(config.outputDirectory, "events.jsonl"), false) { AutoFlush = true };
            Application.logMessageReceived += OnLog;
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            AudioListener.volume = 0;
            AudioListener.pause = true;
            var controllers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(x => x.GetType().FullName == "ArcaneArchive.Player.FirstPersonController").ToArray();
            if (controllers.Length != 1) throw new InvalidOperationException("Expected exactly one existing FirstPersonController.");
            player = controllers[0];
            capsule = player.GetComponent<CharacterController>();
            eye = player.GetComponentInChildren<Camera>();
            if (capsule == null || eye == null) throw new InvalidOperationException("Missing player capsule/camera.");
            var field = player.GetType().GetField("inputActions", BindingFlags.Instance | BindingFlags.NonPublic);
            var asset = field?.GetValue(player) as InputActionAsset;
            actions = asset?.FindActionMap("Player", true);
            if (actions == null) throw new InvalidOperationException("Cannot inspect actual controller actions.");
            var sensitivityField = player.GetType().GetField("mouseSensitivity", BindingFlags.Instance | BindingFlags.NonPublic);
            if (sensitivityField?.GetValue(player) is float sensitivity && !Mathf.Approximately(sensitivity, config.mouseSensitivity))
                throw new InvalidOperationException("Route sensitivity differs from the real controller; review route without overriding controller.");
            if (config.setInitialPosition)
            {
                // Only test setup may place the player; every subsequent traversal uses input events.
                capsule.enabled = false;
                player.transform.SetPositionAndRotation(config.initialPosition, Quaternion.Euler(0, config.initialYaw, 0));
                capsule.enabled = true;
                Physics.SyncTransforms();
            }
            if (config.nearWallFixture)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = "AcceptanceWall_TestOnly";
                wall.transform.SetPositionAndRotation(player.transform.position + player.transform.forward * 3f + Vector3.up * 1.8f, player.transform.rotation);
                wall.transform.localScale = new Vector3(5f, 4f, .2f);
                wall.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                Physics.SyncTransforms();
            }
            summary.scene = player.gameObject.scene.path;
            summary.expectedSteps = config.steps.Length * config.repetitions;
            foreach (var go in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                summary.missingScripts += go.GetComponents<Component>().Count(x => x == null);
            foreach (var behavior in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (behavior.GetType().FullName == "ArcaneArchive.Visuals.MilanMagicMotion")
                {
                    var t = behavior.transform; motionTransforms.Add(t);
                    visual.environmentalMotions.Add(new MotionEvidence { path = HierarchyPath(t), initialPosition = t.localPosition,
                        initialRotation = t.localRotation, initialScale = t.localScale });
                }
            keyboard = InputSystem.AddDevice<Keyboard>("AcceptanceKeyboard");
            mouse = InputSystem.AddDevice<Mouse>("AcceptanceMouse");
            keyboard.MakeCurrent(); mouse.MakeCurrent();
            player.gameObject.AddComponent<AcceptanceContacts>().owner = this;
            stepStart = lastPosition = player.transform.position;
            InputSystem.onBeforeUpdate += Inject;
            initialized = true;
        }
        void Inject()
        {
            if (!initialized || done || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
            float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            var step = Current;
            var keys = (step.keys ?? new string[0]).Select(x => (Key)Enum.Parse(typeof(Key), x, true)).ToList();
            commandedMouse = step.mousePixelsPerSecond * dt;
            if (step.waypoint)
            {
                Vector3 delta = step.target - player.transform.position;
                Key movement = (Key)Enum.Parse(typeof(Key), step.movementKey, true);
                float axisYaw = movement == Key.S ? 180 : movement == Key.A ? -90 : movement == Key.D ? 90 : 0;
                float yaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg - axisYaw;
                float error = Mathf.DeltaAngle(player.transform.eulerAngles.y, yaw);
                commandedMouse.x = Mathf.Clamp(error, -step.turnDegreesPerSecond * dt, step.turnDegreesPerSecond * dt) / config.mouseSensitivity;
                if (Mathf.Abs(error) < 15 && new Vector2(delta.x, delta.z).magnitude > step.horizontalTolerance)
                    keys.Add(movement);
            }
            if (step.setPitch)
            {
                float error = Mathf.DeltaAngle(eye.transform.localEulerAngles.x, step.targetPitch);
                commandedMouse.y = -Mathf.Clamp(error, -step.turnDegreesPerSecond * dt, step.turnDegreesPerSecond * dt) / config.mouseSensitivity;
            }
            commandedKeys = keys.Distinct().ToArray();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(commandedKeys));
            InputSystem.QueueStateEvent(mouse, new MouseState { delta = commandedMouse, buttons = (ushort)(step.leftClick ? 1 : 0) });
        }
        void LateUpdate()
        {
            if (!initialized || done) return;
            try { Observe(); }
            catch (Exception ex) { Debug.LogException(ex); Finish("runner_error"); }
        }
        void Observe()
        {
            float dt = Time.deltaTime;
            var step = Current;
            Vector3 position = player.transform.position;
            Vector3 displacement = position - lastPosition;
            float speed = dt > 0 ? new Vector2(displacement.x, displacement.z).magnitude / dt : 0;
            travelled += new Vector2(displacement.x, displacement.z).magnitude;
            maxSpeed = Mathf.Max(maxSpeed, speed);
            elapsed += dt; frames++;
            float wallDistance = -1;
            foreach (var hit in Physics.RaycastAll(eye.transform.position, eye.transform.forward, 3, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
                if (!hit.transform.IsChildOf(player.transform)) { wallDistance = hit.distance; break; }
            var move = actions.FindAction("Move"); var look = actions.FindAction("Look");
            float eyeAboveGround = -1;
            foreach (var ground in Physics.RaycastAll(eye.transform.position, Vector3.down, 10, ~0, QueryTriggerInteraction.Ignore).OrderBy(h => h.distance))
                if (!ground.transform.IsChildOf(player.transform)) { eyeAboveGround = ground.distance; break; }
            var presentation = player.GetComponentInChildren<Animator>();
            var realController = (ArcaneArchive.Player.FirstPersonController)player;
            samples.WriteLine(JsonUtility.ToJson(new Sample {
                frame = Time.frameCount, time = Time.time, deltaTime = dt, unscaledDeltaTime = Time.unscaledDeltaTime, playerYaw = player.transform.eulerAngles.y, repetition = repetition + 1, step = index,
                id = step.id, cursor = Cursor.lockState.ToString(), position = position, cameraPosition = eye.transform.position,
                cameraEuler = eye.transform.eulerAngles, eyeHeight = eye.transform.position.y - position.y,
                fov = eye.fieldOfView, capsuleHeight = capsule.height, grounded = capsule.isGrounded,
                controllerGrounded = realController.IsGrounded, jumpSequence = realController.JumpSequence, landSequence = realController.LandSequence,
                eyeAboveGround = eyeAboveGround, handStateHash = presentation ? presentation.GetCurrentAnimatorStateInfo(0).shortNameHash : 0,
                handClip = presentation && presentation.GetCurrentAnimatorClipInfo(0).Length > 0 ? presentation.GetCurrentAnimatorClipInfo(0)[0].clip.name : "",
                displacement = displacement, actualSpeed = speed, wallDistance = wallDistance,
                commandedMouse = commandedMouse, commandedKeys = commandedKeys.Select(x => x.ToString()).ToArray(),
                actionMove = move.ReadValue<Vector2>(), actionLook = look.ReadValue<Vector2>(),
                moveDevice = move.activeControl?.device.name, lookDevice = look.activeControl?.device.name,
                actionJump = actions.FindAction("Jump").WasPressedThisFrame(), actionSprint = actions.FindAction("Sprint").IsPressed(),
                actionCrouch = actions.FindAction("Crouch").IsPressed(), touchedColliders = contacts.ToArray()
            }));
            contacts.Clear(); lastPosition = position;
            for (int i = 0; i < motionTransforms.Count; i++)
            {
                var t = motionTransforms[i]; if (t == null) continue;
                var evidence = visual.environmentalMotions[i];
                evidence.currentPosition = t.localPosition; evidence.currentRotation = t.localRotation; evidence.currentScale = t.localScale;
                evidence.observedChange |= Vector3.Distance(t.localPosition, evidence.initialPosition) > 0.0001f ||
                    Quaternion.Angle(t.localRotation, evidence.initialRotation) > 0.01f || Vector3.Distance(t.localScale, evidence.initialScale) > 0.0001f;
            }
            Vector3 delta = step.target - position;
            bool reached = step.waypoint && new Vector2(delta.x, delta.z).magnitude <= step.horizontalTolerance && Mathf.Abs(delta.y) <= step.verticalTolerance;
            if (!reached && elapsed < step.seconds) return;
            bool failed = (step.waypoint && !reached) || travelled < step.minimumTravel || (step.maximumTravel >= 0 && travelled > step.maximumTravel);
            if (failed) summary.failedSteps++;
            summary.steps.Add(new StepResult { id = step.id, repetition = repetition + 1, frames = frames,
                outcome = failed ? "assertion_failed" : "assertions_met_visual_review_pending", start = stepStart, end = position,
                elapsed = elapsed, travelled = travelled, maxObservedSpeed = maxSpeed });
            string evidenceName = $"r{repetition + 1:D2}_s{index:D3}";
            visual.wallRayDistance = wallDistance;
            var viewModel = string.IsNullOrEmpty(config.viewModelRootPath) ? null : GameObject.Find(config.viewModelRootPath);
            visual.viewModelStatus = viewModel == null ? "not_resolved" : "resolved_bounds_only";
            visual.viewModelFurthestForward = -1;
            if (viewModel != null)
                foreach (var renderer in viewModel.GetComponentsInChildren<Renderer>())
                {
                    var bounds = renderer.bounds;
                    for (int corner = 0; corner < 8; corner++)
                    {
                        Vector3 point = bounds.center + Vector3.Scale(bounds.extents, new Vector3((corner & 1) == 0 ? -1 : 1,
                            (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                        visual.viewModelFurthestForward = Mathf.Max(visual.viewModelFurthestForward,
                            Vector3.Dot(point - eye.transform.position, eye.transform.forward));
                    }
                }
            File.WriteAllText(Path.Combine(config.outputDirectory, evidenceName + "_visual.json"), JsonUtility.ToJson(visual, true));
            if (step.screenshot || failed) CaptureCamera(evidenceName);
            File.WriteAllText(Path.Combine(config.outputDirectory, "summary.json"), JsonUtility.ToJson(summary, true));
            if (failed && config.stopOnFailure) { Finish("stopped_after_assertion_failure"); return; }
            index++;
            if (index == config.steps.Length) { index = 0; repetition++; }
            if (repetition == config.repetitions) { Finish("recorded_requires_review"); return; }
            elapsed = travelled = maxSpeed = 0; frames = 0; stepStart = position;
        }
        void CaptureCamera(string name)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            { events.WriteLine("{\"screenshot\":\"unavailable_no_graphics\"}"); return; }
            // Camera frame only; does not claim to capture overlay UI or human visual assessment.
            var old = eye.targetTexture; var active = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(1280, 720, 24);
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try {
                if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
                {
                    var request = new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = rt };
                    UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(eye, request);
                }
                else { eye.targetTexture = rt; eye.Render(); }
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
                File.WriteAllBytes(Path.Combine(config.outputDirectory, name + "_camera.png"), tex.EncodeToPNG());
            } finally { eye.targetTexture = old; RenderTexture.active = active; RenderTexture.ReleaseTemporary(rt); Destroy(tex); }
        }
        [Serializable] sealed class LogEntry { public string type, message, stack; }
        void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Assert) summary.errors++;
            if (type == LogType.Exception) summary.exceptions++;
            if (type != LogType.Log) events?.WriteLine(JsonUtility.ToJson(new LogEntry { type = type.ToString(), message = message, stack = stack }));
        }
        public void Contact(ControllerColliderHit hit) { if (!contacts.Contains(hit.collider.name)) contacts.Add(hit.collider.name); }
        static string HierarchyPath(Transform t) { return t.parent == null ? t.name : HierarchyPath(t.parent) + "/" + t.name; }
        void Finish(string status)
        {
            if (done) return; done = true; summary.status = status;
            summary.complete = summary.steps.Count == summary.expectedSteps;
            File.WriteAllText(Path.Combine(config.outputDirectory, "summary.json"), JsonUtility.ToJson(summary, true));
            Cleanup();
            Completed?.Invoke(status == "runner_error" || summary.errors + summary.exceptions + summary.failedSteps + summary.missingScripts > 0 ? 2 : 0);
        }
        void Cleanup()
        {
            InputSystem.onBeforeUpdate -= Inject; Application.logMessageReceived -= OnLog;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            samples?.Dispose(); events?.Dispose(); samples = events = null;
        }
        void OnDestroy() { Cleanup(); }
    }
    public sealed class AcceptanceContacts : MonoBehaviour
    {
        public MilanAcceptanceRunner owner;
        void OnControllerColliderHit(ControllerColliderHit hit) { owner?.Contact(hit); }
    }
}

#endif
