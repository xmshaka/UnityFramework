﻿﻿
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Linq;
using UniRx;
using Extensions.Devkit;
using Modules.Devkit.EditorSceneChange;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.SceneLaunch
{
    public class SceneLaunchWindow : SingletonEditorWindow<SceneLaunchWindow>
	{
		//----- params -----

		private static class Prefs
        {
			public static string targetScenePath
			{
				get { return ProjectPrefs.GetString("SceneExecuterPrefs-targetScenePath"); }
				set { ProjectPrefs.SetString("SceneExecuterPrefs-targetScenePath", value); }
			}

			public static bool launch
            {
                get { return ProjectPrefs.GetBool("SceneExecuterPrefs-launch", false); }
                set { ProjectPrefs.SetBool("SceneExecuterPrefs-launch", value); }
            }

            public static bool standbyInitializer
            {
                get { return ProjectPrefs.GetBool("SceneExecuterPrefs-standbyInitializer", false); }
                set { ProjectPrefs.SetBool("SceneExecuterPrefs-standbyInitializer", value); }
            }

            public static int[] enableInstanceIds
            {
                get { return ProjectPrefs.Get<int[]>("SceneExecuterPrefs-enableInstanceIds", new int[0]); }
                set { ProjectPrefs.Set<int[]>("SceneExecuterPrefs-enableInstanceIds", value); }
            }
        }

        /// <summary>
        /// 全ヒエラルキーを非アクティブ化.
        /// </summary>
        public static void SuspendSceneInstance()
        {
            var rootObjects = UnityEditorUtility.FindRootObjectsInHierarchy(false);

            // SceneInitializerの初期化を待つ(Awake, Startを走らせない)為.
            // 一時的にHierarchy上のオブジェクトを非アクティブ化.
            foreach (var rootObject in rootObjects)
            {
                rootObject.SetActive(false);
            }

            Prefs.enableInstanceIds = rootObjects.Select(y => y.gameObject.GetInstanceID()).ToArray();
        }

        /// <summary>
        /// 非アクティブ化したObjectを復帰.
        /// </summary>
        public static void ResumeSceneInstance()
        {
            var enableInstanceIds = Prefs.enableInstanceIds;

            var rootObjects = UnityEditorUtility.FindRootObjectsInHierarchy();

            rootObjects = rootObjects.Where(x => enableInstanceIds.Contains(x.gameObject.GetInstanceID())).ToArray();

            // 非アクティブ化したオブジェクトを復元.
            foreach (var rootObject in rootObjects)
            {
                rootObject.SetActive(true);
            }
        }

        [InitializeOnLoad]
        private class SceneResume
        {
            private const int CheckInterval = 180;

            private static int frameCount = 0;
            private static bool forceUpdate = false;
            private static IDisposable disposable = null;

            static SceneResume()
            {
                EditorApplication.update += ResumeScene;
                EditorApplication.playModeStateChanged += PlaymodeStateChanged;
            }

            private static void PlaymodeStateChanged(PlayModeStateChange state)
            {
                if (!Application.isPlaying && Prefs.launch)
                {
                    forceUpdate = true;
                }
            }

            private static void ResumeScene()
            {
                if (Application.isPlaying) { return; }

                if (!Prefs.launch) { return; }

                if (forceUpdate || CheckInterval < frameCount++)
                {
                    var waitScene = EditorSceneChangerPrefs.waitScene;
                    var lastScene = EditorSceneChangerPrefs.lastScene;

                    if (string.IsNullOrEmpty(waitScene) && !string.IsNullOrEmpty(lastScene))
                    {
                        if (disposable != null)
                        {
                            disposable.Dispose();
                            disposable = null;
                        }

                        disposable = EditorSceneChanger.SceneResume(() => Prefs.launch = false).Subscribe();
                    }
                    // 現在のシーンから起動されたのでResumeされない.
                    else
                    {
                        Prefs.launch = false;
                        ResumeSceneInstance();
                    }

                    frameCount = 0;
                    forceUpdate = false;
                }
            }
        }

		//----- field -----

		private string targetScenePath = null;

		private bool initialized = false;

		//----- property -----

		//----- method -----

		public static void Open()
		{
			Instance.titleContent = new GUIContent("Launch Scene");
			Instance.minSize = new Vector2(0f, 60f);

			Instance.Initialize();

			Instance.Show();
		}

		public void Initialize()
		{
			if (initialized) { return; }

			targetScenePath = Prefs.targetScenePath;

			initialized = true;
		}

		void OnEnable()
		{
			Initialize();
		}

		void Update()
		{
			if (!initialized)
			{
				Initialize();
			}
		}

		void OnGUI()
		{
			EditorLayoutTools.SetLabelWidth(120f);

            EditorGUILayout.Separator();

            using(new EditorGUILayout.HorizontalScope())
            {
                var sceneName = Path.GetFileName(targetScenePath);

                if (EditorLayoutTools.DrawPrefixButton("Scene", GUILayout.Width(65f)))
                {
                    SceneSelectorPrefs.selectedScenePath = targetScenePath;
                    SceneSelector.Open().Subscribe(OnSelectScene);
                }

                var sceneNameStyle = GUI.skin.GetStyle("TextArea");
                sceneNameStyle.alignment = TextAnchor.MiddleLeft;

				EditorGUILayout.SelectableLabel(sceneName, sceneNameStyle, GUILayout.Height(18f));

				GUILayout.Space(5f);
            }

			GUILayout.Space(5f);

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Space(5f);

				// 下記条件時は再生ボタンを非アクティブ:.
				// ・実行中.
				// ・ビルド中.
				// ・遷移先シーンが未選択.
				GUI.enabled = !(EditorApplication.isPlaying ||
							EditorApplication.isCompiling ||
							string.IsNullOrEmpty(targetScenePath));

				if (GUILayout.Button("Launch"))
				{
					Launch().Subscribe();
				}

				GUI.enabled = true;

				GUILayout.Space(5f);
			}
		}

        private IObservable<Unit> Launch()
        {
            return EditorSceneChanger.SceneChange(targetScenePath)
                .Do(x =>
                    {
                        if (x)
                        {
                            Prefs.launch = true;
                            Prefs.standbyInitializer = true;

                            SuspendSceneInstance();

                            // 実行状態にする.
                            // ※ 次のフレームでメモリ内容が消滅する.
                            EditorApplication.isPlaying = true;
                        }
                    })
                .AsUnitObservable();
        }


        [InitializeOnLoadMethod()]
        private static void InitializeOnLoadMethod()
        {
            if (Prefs.standbyInitializer)
            {
                EditorApplication.CallbackFunction execCallbackFunction = null;

                execCallbackFunction = () =>
                {
                    ExecSceneInitializer().Subscribe();

                    EditorApplication.delayCall -= execCallbackFunction;
                };

                EditorApplication.delayCall += execCallbackFunction;
                
                Prefs.standbyInitializer = false;
            }
        }

        private static IObservable<Unit> ExecSceneInitializer()
        {
            // ScriptableObjectの初期化を待つ為1フレーム待機.
            return Observable.NextFrame()
                .Do(_ => ResumeSceneInstance())
                .AsUnitObservable();
        }

        private void OnSelectScene(string targetScenePath)
        {
			this.targetScenePath = targetScenePath;

			Prefs.targetScenePath = targetScenePath;

            SceneSelectorPrefs.selectedScenePath = targetScenePath;

            Repaint();
        }

        private static string GetCurrentScenePath()
        {
            var scene = EditorSceneManager.GetSceneAt(0);
            return scene.path;
        }

        private string AsSpacedCamelCase(string text)
        {
            var sb = new System.Text.StringBuilder(text.Length * 2);
            sb.Append(char.ToUpper(text[0]));

            for (var i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]) && text[i - 1] != ' ')
                    sb.Append(' ');
                sb.Append(text[i]);
            }
            return sb.ToString();
        }
    }
}
