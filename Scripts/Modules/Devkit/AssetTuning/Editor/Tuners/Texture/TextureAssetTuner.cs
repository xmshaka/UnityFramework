﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;
using Extensions;
using Object = UnityEngine.Object;

namespace Modules.Devkit.AssetTuning
{
    public class TextureAssetTuner : AssetTuner
    {
        //----- params -----

        private static readonly BuildTargetGroup[] DefaultTargetPlatforms =
        {
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS,
        };

        //----- field -----
        
        //----- property -----
        
        /// <summary> 適用対象 </summary>
        protected virtual BuildTargetGroup[] Platforms
        {
            get { return DefaultTargetPlatforms; }
        }

        //----- method -----
       
        public override bool Validate(string assetPath)
        {
            var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            return textureImporter != null;
        }

        public override void OnAssetCreate(string assetPath)
        {
            var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (textureImporter == null) { return; }

            OnFirstImport(textureImporter);

            textureImporter.SaveAndReimport();
        }

        public virtual void OnPreprocessTexture(string assetPath, bool isFirstImport)
        {
            var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (textureImporter == null) { return; }

            if (isFirstImport)
            {
                OnFirstImport(textureImporter);
            }
            else
            {
                SetTextureTypeSettings(textureImporter);
                SetCompressionSettings(textureImporter);
            }
        }

        protected virtual void OnFirstImport(TextureImporter textureImporter)
        {
            if (textureImporter == null) { return; }

            textureImporter.alphaSource = TextureImporterAlphaSource.FromInput;
            textureImporter.alphaIsTransparency = true;
            textureImporter.mipmapEnabled = false;
            textureImporter.isReadable = false;
            textureImporter.npotScale = TextureImporterNPOTScale.None;

            var settings = new TextureImporterSettings();

            textureImporter.ReadTextureSettings(settings);

            settings.spriteGenerateFallbackPhysicsShape = false;

            textureImporter.SetTextureSettings(settings);

            SetTextureTypeSettings(textureImporter);

            SetCompressionSettings(textureImporter);
        }

        protected virtual void SetCompressionSettings(TextureImporter textureImporter)
        {
            var config = TextureAssetTunerConfig.Instance;

            if (config == null) { return; }

            if (!IsFolderItem(textureImporter.assetPath, config.CompressFolders, config.IgnoreCompressFolderNames))
            {
                return;
            }

            var size = textureImporter.GetPreImportTextureSize();

            // ブロックが使えるか(4の倍数なら圧縮設定).
            var isMultipleOf4 = IsMultipleOf4(size.x) && IsMultipleOf4(size.y);

            if (!isMultipleOf4)
            {
                SetDefaultSettings(textureImporter);
                return;
            }

            foreach (var platform in Platforms)
            {
                Func<TextureImporterPlatformSettings, TextureImporterPlatformSettings> update = settings =>
                {
                    settings.overridden = true;
                    settings.compressionQuality = (int)UnityEngine.TextureCompressionQuality.Normal;
                    settings.textureCompression = TextureImporterCompression.Compressed;
                    settings.androidETC2FallbackOverride = AndroidETC2FallbackOverride.UseBuildSettings;
                    settings.format = GetPlatformCompressionType(textureImporter, platform);

                    return settings;
                };

                textureImporter.SetPlatformTextureSetting(platform, update);
            }
        }

        protected virtual bool SetTextureTypeSettings(TextureImporter textureImporter)
        {
            var config = TextureAssetTunerConfig.Instance;

            if (config == null) { return false; }

            var isTarget = false;

            var parts = textureImporter.assetPath.Split(PathUtility.PathSeparator);

            isTarget |= parts.Any(x => config.SpriteFolderNames.Contains(x));

            isTarget |= IsFolderItem(textureImporter.assetPath, config.SpriteFolders, config.IgnoreSpriteFolderNames);

            if (isTarget)
            {
                textureImporter.textureType = TextureImporterType.Sprite;
            }

            return isTarget;
        }

        protected virtual TextureImporterFormat GetPlatformCompressionType(TextureImporter textureImporter, BuildTargetGroup platform)
        {
            var format = TextureImporterFormat.RGBA32;

            var hasAlpha = textureImporter.DoesSourceTextureHaveAlpha();

            switch (platform)
            {
                case BuildTargetGroup.iOS:
                    format = hasAlpha ? TextureImporterFormat.ASTC_RGBA_4x4 : TextureImporterFormat.ASTC_RGB_4x4;
                    break;

                case BuildTargetGroup.Android:
                    format = hasAlpha ? TextureImporterFormat.ASTC_RGBA_4x4 : TextureImporterFormat.ASTC_RGB_4x4;
                    break;
            }

            return format;
		}

        protected void SetDefaultSettings(TextureImporter textureImporter)
        {
            foreach (var platform in Platforms)
            {
                var platformTextureSetting = textureImporter.GetPlatformTextureSettings(platform.ToString());

                Func<TextureImporterPlatformSettings, TextureImporterPlatformSettings> update = settings =>
                {
                    platformTextureSetting.overridden = false;

                    return platformTextureSetting;
                };

                textureImporter.SetPlatformTextureSetting(platform, update);
            }
        }

        public static bool IsFolderItem(string assetPath, Object[] folders, string[] ignoreFolderNames)
        {
            assetPath = PathUtility.ConvertPathSeparator(assetPath);

            var targetPaths = folders.Where(x => x != null).Select(x => AssetDatabase.GetAssetPath(x));

            foreach (var targetPath in targetPaths)
            {
                var path = PathUtility.ConvertPathSeparator(targetPath);

                if (assetPath.StartsWith(path + PathUtility.PathSeparator))
                {
                    var parts = assetPath.Substring(path.Length).Split(PathUtility.PathSeparator);

                    if (parts.All(x => !ignoreFolderNames.Contains(x)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsMultipleOf4(float value)
        {
            return value % 4 == 0;
        }
    }
}
