using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Door13.EditorTools
{
    /// <summary>
    /// batchmode Play 冒烟测试（上项目教训：编译通过≠能运行）。
    /// 用法（会话不要加 -quit，由本测试内部 EditorApplication.Exit 结束）：
    ///   Unity -batchmode -nographics -projectPath … -executeMethod Door13.EditorTools.SmokeTest.Run
    /// 真实进入播放模式跑帧，捕获全部 Error/Exception，异常或超时以非零码退出。
    ///
    /// 关键：进入播放模式会触发域重载，静态事件订阅在重载后丢失——
    /// 用 SessionState 标记 + [InitializeOnLoad] 静态构造在重载后恢复订阅，
    /// 否则测试会假死（播放已启动但无人计数）。
    /// </summary>
    [InitializeOnLoad]
    public static class SmokeTest
    {
        const int MinFrames = 240;      // 至少跑的帧数
        const float MaxSeconds = 60f;   // 播放模式总超时
        const float StartTimeout = 25f; // 进入播放模式超时

        static int frames;
        static float startTime;
        static readonly List<string> errors = new List<string>();

        static SmokeTest()
        {
            // 域重载后恢复订阅（仅当冒烟测试正在运行）
            if (SessionState.GetBool("SMOKE_RUNNING", false))
                Subscribe();
        }

        static void Subscribe()
        {
            startTime = (float)EditorApplication.timeSinceStartup;
            Application.logMessageReceived += OnLog;
            EditorApplication.update += OnUpdate;
        }

        public static void Run()
        {
            string full = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                ProjectSetup.ScenePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(full))
            {
                Debug.LogError("SMOKE_FAIL 场景不存在，请先执行 SetupOnly: " + ProjectSetup.ScenePath);
                EditorApplication.Exit(3);
                return;
            }

            EditorSceneManager.OpenScene(ProjectSetup.ScenePath);
            SessionState.SetBool("SMOKE_RUNNING", true);
            Subscribe();
            Debug.Log("SMOKE_START 进入播放模式冒烟测试…");
            EditorApplication.isPlaying = true;
        }

        static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
                errors.Add(condition);
        }

        static void OnUpdate()
        {
            float secs = (float)(EditorApplication.timeSinceStartup - startTime);

            if (!EditorApplication.isPlaying)
            {
                // 域重载期间 isPlaying 短暂为 false，属正常；从未进入过播放模式才算失败
                if (secs > StartTimeout && !SessionState.GetBool("SMOKE_PLAYED", false))
                {
                    Debug.LogError("SMOKE_FAIL 播放模式未能启动");
                    Exit(2);
                }
                return;
            }

            SessionState.SetBool("SMOKE_PLAYED", true);
            frames++;
            if (errors.Count > 0 || frames >= MinFrames || secs > MaxSeconds)
            {
                Debug.Log($"SMOKE_RESULT frames={frames} seconds={secs:F1} errors={errors.Count}");
                foreach (var e in errors)
                    Debug.LogError("SMOKE_ERROR: " + e);
                Exit(errors.Count > 0 ? 1 : 0);
            }
        }

        static void Exit(int code)
        {
            SessionState.SetBool("SMOKE_RUNNING", false);
            SessionState.SetBool("SMOKE_PLAYED", false);
            EditorApplication.isPlaying = false;
            Application.logMessageReceived -= OnLog;
            EditorApplication.update -= OnUpdate;
            EditorApplication.Exit(code);
        }
    }
}
