using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Door13.EditorTools
{
    /// <summary>
    /// 一键构建流水线（batchmode -executeMethod 入口）：
    /// 工具链登记（EditorPrefs + 反射写入 AndroidExternalToolsSettings）→ APK 输出。
    /// 安卓工具不支持非 ASCII 路径：构建统一经由 ASCII 联接 D:\TDM 打开工程。
    /// </summary>
    public static class BuildDoor13
    {
        [MenuItem("Door13/3-仅配置(设置+场景+工具链)")]
        public static void SetupOnly()
        {
            ProjectSetup.ApplyPlayerSettings();
            ProjectSetup.EnsureScene();
            ConfigureAndroidTools();
        }

        [MenuItem("Door13/0-仅写工具链配置(重启会话生效)")]
        public static void ConfigureOnly() => ConfigureAndroidTools();

        [MenuItem("Door13/4-仅打包APK")]
        public static void BuildApkOnly()
        {
            // 输出用 ASCII 文件名（构建工具限制），打包成功后由外部脚本改为中文名交付
            string outPath = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName, "Build", "Door13.apk");
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.ScenePath },
                locationPathName = outPath,
                target = BuildTarget.Android,
                options = BuildOptions.CompressWithLz4HC
            };
            BuildReport report = BuildPipeline.BuildPlayer(opts);
            Debug.Log($"BUILD_RESULT {report.summary.result} size={report.summary.totalSize} path={outPath}");
            if (Application.isBatchMode)
                EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        static void ConfigureAndroidTools()
        {
            // 工具链路径：优先 ASCII 联接 D:\UBSAndroid，回退统一环境目录，再回退编辑器内嵌
            string androidRoot = Directory.Exists(@"D:\UBSAndroid")
                ? @"D:\UBSAndroid"
                : (Directory.Exists(@"D:\Agent工作区\Unity开发环境\Android")
                    ? @"D:\Agent工作区\Unity开发环境\Android"
                    : Path.Combine(
                        Directory.GetParent(EditorApplication.applicationPath).FullName,
                        "Data", "PlaybackEngines", "AndroidPlayer"));
            string sdk = Path.Combine(androidRoot, "SDK");
            string ndk = Path.Combine(androidRoot, "NDK");
            string jdk = Path.Combine(androidRoot, "OpenJDK");

            EditorPrefs.SetBool("JdkUseEmbedded", false);
            EditorPrefs.SetBool("SdkUseEmbedded", false);
            EditorPrefs.SetBool("NdkUseEmbedded", false);
            if (Directory.Exists(jdk)) EditorPrefs.SetString("JdkPath", jdk);
            if (Directory.Exists(sdk)) EditorPrefs.SetString("AndroidSdkRoot", sdk);
            if (Directory.Exists(ndk)) EditorPrefs.SetString("AndroidNdkRoot", ndk);

            // Unity 2022.3.62c1 不读 JdkPath/AndroidNdkRoot 注册表键，反射写属性最可靠
            TrySetAndroidToolSetting("jdkRootPath", jdk, "JDK");
            TrySetAndroidToolSetting("sdkRootPath", sdk, "SDK");
            TrySetAndroidToolSetting("ndkRootPath", ndk, "NDK");
        }

        static void TrySetAndroidToolSetting(string prop, string value, string label)
        {
            try
            {
                Type t = null;
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    t = a.GetType("UnityEditor.Android.AndroidExternalToolsSettings");
                    if (t != null) break;
                }
                if (t == null) { Debug.LogWarning($"TOOLSETTING {label}: 类未找到"); return; }
                var p = t.GetProperty(prop, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (p == null) { Debug.LogWarning($"TOOLSETTING {label}: 属性 {prop} 未找到"); return; }
                p.SetValue(null, value);
                Debug.Log($"TOOLSETTING {label} -> {p.GetValue(null)}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"TOOLSETTING {label} 失败: {e.Message}");
            }
        }
    }
}
