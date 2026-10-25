using System.IO;
using Door13.Player;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Door13.EditorTools
{
    /// <summary>
    /// 工程一键配置：玩家设置（安卓/IL2CPP/GLES3/横屏）+ 生成测试场景。
    /// 场景全部由代码生成（纯自制资源原则），可在菜单反复重建。
    /// </summary>
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";
        const string CirclePng = "Assets/UI/Tex_Circle.png";

        [MenuItem("Door13/1-工程配置+生成测试场景")]
        public static void SetupAll()
        {
            ApplyPlayerSettings();
            EnsureScene();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Door13/2-仅重新生成测试场景")]
        public static void RegenerateScene()
        {
            EnsureScene();
            AssetDatabase.SaveAssets();
        }

        /// <summary>安卓玩家设置（沿用上项目验证过的配置）</summary>
        public static void ApplyPlayerSettings()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.Android);

            PlayerSettings.companyName = "Door13 Studio";
            PlayerSettings.productName = "第十三扇门：失忆档案";
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Android, "com.door13.amnesia");
            PlayerSettings.bundleVersion = "0.1.1";
            PlayerSettings.Android.bundleVersionCode = 2;

            // 画质：4x MSAA 抗锯齿 + 高质量阴影（避免真机画面/物体发糊）
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = 20f;
            QualitySettings.shadowCascades = 1;

            PlayerSettings.SetScriptingBackend(
                NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(
                NamedBuildTarget.Android, ManagedStrippingLevel.Medium);
            PlayerSettings.colorSpace = ColorSpace.Gamma;

            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
                new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.optimizedFramePacing = true;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            EditorUserBuildSettings.buildAppBundle = false;
        }

        /// <summary>生成（或重建）测试场景：房间+灯光+玩家+UI</summary>
        public static void EnsureScene()
        {
            EnsureFolders();
            Sprite circle = EnsureCircleSprite();

            var scene = File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildRoom();
            BuildLighting();
            BuildPlayer();
            BuildUI(circle);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("SCENE_OK 测试场景已生成: " + ScenePath);
        }

        static void EnsureFolders()
        {
            string[] folders = { "Scenes", "Materials", "UI", "Prefabs", "Models", "Audio", "Data" };
            foreach (var f in folders)
                if (!AssetDatabase.IsValidFolder("Assets/" + f))
                    AssetDatabase.CreateFolder("Assets", f);
        }

        static Material EnsureMaterial(string path, Color color)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.color = color;
            mat.SetFloat("_Glossiness", 0f);
            return mat;
        }

        /// <summary>程序化生成圆形贴图（UI用），零外部资源</summary>
        static Sprite EnsureCircleSprite()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePng);
            if (sprite != null) return sprite;

            const int size = 96;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = 44f, c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((r - d) / 2f)));
                }
            tex.Apply();
            File.WriteAllBytes(Application.dataPath + CirclePng.Substring("Assets".Length), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(CirclePng);
            var imp = (TextureImporter)AssetImporter.GetAtPath(CirclePng);
            imp.textureType = TextureImporterType.Sprite;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(CirclePng);
        }

        static GameObject Box(string name, Vector3 center, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.isStatic = true;
            go.transform.position = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static void BuildRoom()
        {
            var room = new GameObject("TestRoom");

            var wall = EnsureMaterial("Assets/Materials/M_Wall.mat", new Color(0.32f, 0.34f, 0.38f));
            var floor = EnsureMaterial("Assets/Materials/M_Floor.mat", new Color(0.22f, 0.23f, 0.26f));
            var prop = EnsureMaterial("Assets/Materials/M_Prop.mat", new Color(0.45f, 0.34f, 0.24f));
            var door = EnsureMaterial("Assets/Materials/M_Door.mat", new Color(0.10f, 0.11f, 0.13f));

            Box("Floor", new Vector3(0f, -0.1f, 0f), new Vector3(16f, 0.2f, 16f), floor).transform.SetParent(room.transform);
            var ceil = Box("Ceiling", new Vector3(0f, 3.3f, 0f), new Vector3(16f, 0.2f, 16f), wall);
            ceil.transform.SetParent(room.transform);
            // 天花板不投影，避免遮挡天光导致室内全黑
            ceil.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Box("Wall_N", new Vector3(0f, 1.6f, -8.1f), new Vector3(16.4f, 3.2f, 0.2f), wall).transform.SetParent(room.transform);
            Box("Wall_S", new Vector3(0f, 1.6f, 8.1f), new Vector3(16.4f, 3.2f, 0.2f), wall).transform.SetParent(room.transform);
            Box("Wall_W", new Vector3(-8.1f, 1.6f, 0f), new Vector3(0.2f, 3.2f, 16.4f), wall).transform.SetParent(room.transform);
            Box("Wall_E", new Vector3(8.1f, 1.6f, 0f), new Vector3(0.2f, 3.2f, 16.4f), wall).transform.SetParent(room.transform);

            // 门（视觉占位，阶段2接交互）
            Box("Door", new Vector3(0f, 1.1f, -7.95f), new Vector3(1.6f, 2.2f, 0.25f), door).transform.SetParent(room.transform);

            // 参照物：木箱与立柱
            Box("Crate_1", new Vector3(2f, 0.5f, 3f), new Vector3(1f, 1f, 1f), prop).transform.SetParent(room.transform);
            Box("Crate_2", new Vector3(2.7f, 0.4f, -2.2f), new Vector3(0.8f, 0.8f, 0.8f), prop).transform.SetParent(room.transform);
            Box("Crate_3", new Vector3(2.7f, 1.2f, -2.2f), new Vector3(0.7f, 0.6f, 0.7f), prop).transform.SetParent(room.transform);
            Box("Pillar_1", new Vector3(-4f, 1.6f, -4f), new Vector3(0.8f, 3.2f, 0.8f), wall).transform.SetParent(room.transform);
            Box("Pillar_2", new Vector3(-4f, 1.6f, 4f), new Vector3(0.8f, 3.2f, 0.8f), wall).transform.SetParent(room.transform);
        }

        static void BuildLighting()
        {
            var sunGo = new GameObject("DirLight");
            var l = sunGo.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(0.72f, 0.80f, 0.95f);
            l.intensity = 1.0f;
            l.shadows = LightShadows.Soft;
            l.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;
            sunGo.transform.rotation = Quaternion.Euler(55f, 310f, 0f);

            // 夜色氛围但保持可读性：环境光足够看清结构，雾只作远处淡出
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.28f, 0.30f, 0.36f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.06f, 0.07f, 0.10f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 10f;
            RenderSettings.fogEndDistance = 45f;
        }

        static void BuildPlayer()
        {
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0.05f, 4f);
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // 面向门

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.7f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.85f, 0f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            body.transform.localScale = new Vector3(0.55f, 0.85f, 0.55f);
            body.GetComponent<Collider>().enabled = false; // 碰撞由 CharacterController 负责
            body.GetComponent<Renderer>().sharedMaterial =
                EnsureMaterial("Assets/Materials/M_Player.mat", new Color(0.15f, 0.15f, 0.18f));

            var head = new GameObject("Head");
            head.transform.SetParent(player.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.55f, 0f);

            var camGo = new GameObject("MainCamera");
            camGo.transform.SetParent(head.transform, false);
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 60f;
            cam.fieldOfView = 70f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.10f);
            camGo.AddComponent<AudioListener>();

            var pc = player.AddComponent<PlayerController>();
            pc.head = head.transform;

            var mi = player.AddComponent<MobileInput>();
            _playerInput = mi;
        }

        static MobileInput _playerInput;

        static void BuildUI(Sprite circle)
        {
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var lookZone = canvasGo.AddComponent<TouchLookZone>();

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // 虚拟摇杆（左下）
            var baseGo = new GameObject("JoystickBase");
            baseGo.transform.SetParent(canvasGo.transform, false);
            var baseImg = baseGo.AddComponent<Image>();
            baseImg.sprite = circle;
            baseImg.color = new Color(1f, 1f, 1f, 0.28f);
            var baseRt = (RectTransform)baseGo.transform;
            baseRt.anchorMin = baseRt.anchorMax = Vector2.zero;
            baseRt.anchoredPosition = new Vector2(240f, 240f);
            baseRt.sizeDelta = new Vector2(280f, 280f);

            var knobGo = new GameObject("Knob");
            knobGo.transform.SetParent(baseGo.transform, false);
            var knobImg = knobGo.AddComponent<Image>();
            knobImg.sprite = circle;
            knobImg.color = new Color(1f, 1f, 1f, 0.5f);
            var knobRt = (RectTransform)knobGo.transform;
            knobRt.anchorMin = knobRt.anchorMax = new Vector2(0.5f, 0.5f);
            knobRt.sizeDelta = new Vector2(130f, 130f);

            var joy = baseGo.AddComponent<VirtualJoystick>();
            joy.knob = knobRt;
            joy.radius = 90f;

            // 准星（不拦截射线，保证中央触摸可转视角）
            var crossGo = new GameObject("Crosshair");
            crossGo.transform.SetParent(canvasGo.transform, false);
            var crossImg = crossGo.AddComponent<Image>();
            crossImg.sprite = circle;
            crossImg.color = new Color(1f, 1f, 1f, 0.55f);
            crossImg.raycastTarget = false;
            var crossRt = (RectTransform)crossGo.transform;
            crossRt.anchorMin = crossRt.anchorMax = new Vector2(0.5f, 0.5f);
            crossRt.sizeDelta = new Vector2(10f, 10f);

            // 操作提示
            var hintGo = new GameObject("HintText");
            hintGo.transform.SetParent(canvasGo.transform, false);
            var hint = hintGo.AddComponent<Text>();
            hint.font = GetBuiltinFont();
            hint.text = "摇杆移动 | 拖动屏幕转视角 | 编辑器: WASD + 右键拖动 | 测试房间 v0.1";
            hint.fontSize = 30;
            hint.color = new Color(0.8f, 0.8f, 0.85f, 0.9f);
            hint.alignment = TextAnchor.UpperLeft;
            hint.raycastTarget = false;
            var hintRt = (RectTransform)hintGo.transform;
            hintRt.anchorMin = new Vector2(0f, 1f);
            hintRt.anchorMax = new Vector2(1f, 1f);
            hintRt.pivot = new Vector2(0f, 1f);
            hintRt.anchoredPosition = new Vector2(20f, -16f);
            hintRt.sizeDelta = new Vector2(-40f, 44f);

            if (_playerInput != null)
            {
                _playerInput.joystick = joy;
                _playerInput.lookZone = lookZone;
            }
        }

        static Font GetBuiltinFont()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            return f;
        }
    }
}
