﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Linq;
using UniRx;
using Extensions;
using Modules.Devkit.EventHook;

namespace Modules.Devkit.CleanComponent
{
    #if !UNITY_2018_3_OR_NEWER

    public sealed class PrefabParticleComponentCleaner : ParticleComponentCleaner
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            PrefabApplyHook.OnApplyPrefabAsObservable().Subscribe(x => OnApplyPrefab(x));
        }

        private static void OnApplyPrefab(GameObject prefab)
        {
            if (prefab == null) { return; }

            if (!Prefs.autoClean) { return; }

            var gameObjects = prefab.DescendantsAndSelf().ToArray();

            if (!CheckExecute(gameObjects)) { return; }

            foreach (var gameObject in gameObjects)
            {
                ModifyParticleSystemComponent(gameObject);
            }
        }
    }

    #endif
}
