using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using LevelImposter.Core;
using Newtonsoft.Json;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Il2CppInterop.Runtime.Attributes;

namespace LevelImposter.Shop
{
    /// <summary>
    /// API to manage thumbnail files in the local filesystem
    /// </summary>
    public class ThumbnailFileAPI : FileCache
    {
        public ThumbnailFileAPI(IntPtr intPtr) : base(intPtr)
        {
        }

        public static ThumbnailFileAPI? Instance = null;

        /// <summary>
        /// Reads and parses a thumbnail file into a Texture2D.
        /// </summary>
        /// <param name="mapID">Map ID for the thumbnail</param>
        /// <param name="callback">Callback on success</param>
        [HideFromIl2Cpp]
        public void Get(string mapID, Action<Sprite?> callback)
        {
            if (!Exists(mapID))
            {
                LILogger.Warn($"Could not find [{mapID}] thumbnail in filesystem");
                return;
            }

            LILogger.Info($"Loading thumbnail [{mapID}] from filesystem");
            bool isInCache = SpriteLoader.Instance?.IsSpriteInCache(mapID) ?? false;
            byte[] thumbnailBytes = !isInCache ? (Get(mapID) ?? new byte[0]) : new byte[0];
            SpriteLoader.Instance?.LoadSpriteAsync(thumbnailBytes, false, (spriteData) =>
            {
                Sprite? sprite = spriteData?.Sprite;
                if (sprite == null)
                {
                    LILogger.Warn($"Error loading [{mapID}] thumbnail from filesystem");
                    return;
                }
                callback.Invoke(spriteData?.Sprite);
            }, mapID);
        }

        /// <summary>
        /// Finds and deletes the old thumbnail directory
        /// </summary>
        private static void DeleteLegacyDir()
        {
            string gameDir = System.Reflection.Assembly.GetAssembly(typeof(LevelImposter))?.Location ?? "/";
            string legacyDir = Path.Combine(Path.GetDirectoryName(gameDir) ?? "/", "LevelImposter/Thumbnails");
            if (Directory.Exists(legacyDir))
                Directory.Delete(legacyDir, true);
        }

        public void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                DeleteLegacyDir();
                SetFileExtension(".png");
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}