﻿
using UnityEngine;
using Modules.Devkit.ScriptableObjects;

using Object = UnityEngine.Object;

namespace Modules.Devkit.AssetTuning
{
    public sealed class TextureAssetTunerConfig : ReloadableScriptableObject<TextureAssetTunerConfig>
    {
        //----- params -----

        //----- field -----

        // compress
        
        [SerializeField]
        private Object[] compressFolders = null;
        [SerializeField]
        private string[] ignoreCompressFolderNames = null;

        // sprite

        [SerializeField]
        private Object[] spriteFolders = null;
        [SerializeField]
        private string[] spriteFolderNames = null;
        [SerializeField]
        private string[] ignoreSpriteFolderNames = null;

        //----- property -----

        /// <summary> 圧縮設定を適用するフォルダ. </summary>
        public Object[] CompressFolders
        {
            get { return compressFolders ?? (compressFolders = new Object[0]); }
        }

        /// <summary> 圧縮設定の適用から除外するフォルダ名. </summary>
        public string[] IgnoreCompressFolderNames
        {
            get { return ignoreCompressFolderNames ?? (ignoreCompressFolderNames = new string[0]); }
        }

        /// <summary> TextureTypeをSpriteに設定するフォルダ. </summary>
        public Object[] SpriteFolders
        {
            get { return spriteFolders ?? (spriteFolders = new Object[0]); }
        }

        /// <summary> TextureTypeをSpriteに設定するフォルダ名. </summary>
        public string[] SpriteFolderNames
        {
            get { return spriteFolderNames ?? (spriteFolderNames = new string[0]); }
        }

        /// <summary> TextureTypeをSpriteに設定適用から除外するフォルダ名. </summary>
        public string[] IgnoreSpriteFolderNames
        {
            get { return ignoreSpriteFolderNames ?? (ignoreSpriteFolderNames = new string[0]); }
        }

        //----- method -----
    }
}
