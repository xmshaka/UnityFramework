﻿
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Unity.Linq;
using System.Linq;
using Extensions;
using Modules.Devkit.Prefs;
using Modules.GameText.Components;
using Modules.UI.Extension;

namespace Modules.Devkit.CleanComponent
{
    public abstract class TextComponentCleaner
    {
        //----- params -----

        public static class Prefs
        {
            public static bool autoClean
            {
                get { return ProjectPrefs.GetBool("TextComponentCleanerPrefs-autoClean", true); }
                set { ProjectPrefs.SetBool("TextComponentCleanerPrefs-autoClean", value); }
            }
        }

        //----- field -----

        //----- property -----

        //----- method -----

        protected static void ModifyTextComponent(GameObject rootObject)
        {
            var textComponents = rootObject.DescendantsAndSelf().OfComponent<Text>();

            foreach (var textComponent in textComponents)
            {
                if (string.IsNullOrEmpty(textComponent.text)) { continue; }

                textComponent.text = string.Empty;

                var gameTextSetter = UnityUtility.GetComponent<GameTextSetter>(textComponent);

                if (gameTextSetter != null)
                {
                    gameTextSetter.ImportText();
                }

                EditorUtility.SetDirty(textComponent);
            }
        }

        protected static bool CheckExecute(GameObject[] gameObjects)
        {
            var modify = gameObjects.SelectMany(x => x.DescendantsAndSelf().OfComponent<Text>())
                .Where(x => !string.IsNullOrEmpty(x.text))
                .Where(x =>
                    {
                        var execute = true;

                        var gameTextSetter = UnityUtility.GetComponent<GameTextSetter>(x);

                        if (gameTextSetter != null)
                        {
                            if (gameTextSetter.Content == x.text)
                            {
                                execute = false;
                            }

                            if (gameTextSetter.GetDevelopmentText() == x.text)
                            {
                                execute = false;
                            }
                        }

                        return execute;
                    })
                .Any();

            if (modify)
            {
                return EditorUtility.DisplayDialog("TextComponent Cleaner", "Text is set directly Do you want to run cleanup?", "Clean", "Keep");
            }

            return false;
        }
    }
}
