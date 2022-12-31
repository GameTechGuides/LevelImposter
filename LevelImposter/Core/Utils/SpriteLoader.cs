﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using LevelImposter.Shop;
using LevelImposter.DB;
using System.Diagnostics;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using System.Collections;
using System.Threading;

namespace LevelImposter.Core
{
    /// <summary>
    /// Multi-threaded sprite loader
    /// </summary>
    public class SpriteLoader : MonoBehaviour
    {
        public SpriteLoader(IntPtr intPtr) : base(intPtr)
        {
        }

        private bool _canRender = true;
        private int _renderCount = 0;

        public int RenderCount => _renderCount;
        public delegate void SpriteEventHandle(LIElement elem);
        public event SpriteEventHandle OnLoad;

        public static SpriteLoader Instance;

        public void Awake()
        {
            Instance = this;
        }
        public void LateUpdate()
        {
            _canRender = true;
        }

        /// <summary>
        /// Loads a custom sprite from an
        /// LIElement and applies it to an object
        /// </summary>
        /// <param name="element">LIElement to grab sprite from</param>
        /// <param name="obj">GameObject to apply sprite data to</param>
        public void LoadSprite(LIElement element, GameObject obj)
        {
            LILogger.Info($"Loading sprite for {element}");
            string b64 = element.properties.spriteData ?? "";
            Color spriteColor = MapUtils.LIColorToColor(element.properties.color);
            LoadSprite(b64, (sprite) =>
            {
                SpriteRenderer spriteRenderer = obj.GetComponent<SpriteRenderer>();
                spriteRenderer.sprite = sprite;
                spriteRenderer.color = spriteColor;
                LILogger.Info($"Done loading sprite for {element}");
                if (OnLoad != null)
                    OnLoad.Invoke(element);
            });
        }

        /// <summary>
        /// Loads the custom sprite in a seperate thread
        /// </summary>
        /// <param name="b64">LIElement to read</param>
        /// <param name="onLoad">Callback on success</param>
        public void LoadSprite(string b64Image, Action<Sprite> onLoad)
        {
            if (LIShipStatus.Instance == null)
            {
                LILogger.Error("Cannot load sprite, LIShipStatus.Instance is null");
                return;
            }
            StartCoroutine(CoLoadElement(b64Image, onLoad).WrapToIl2Cpp());
        }

        private IEnumerator CoLoadElement(string b64Image, Action<Sprite> onLoad)
        {
            _renderCount++;
            Task<TextureMetadata> task = Task.Run(() => { return ProcessImage(b64Image); });
            yield return null;
            while (!task.IsCompleted || !_canRender)
                yield return null;
            TextureMetadata texData = task.Result;
            _canRender = false;
            Sprite sprite = LoadImage(texData);
            onLoad.Invoke(sprite);
            _renderCount--;
        }

        /// <summary>
        /// Thread-safe image processer
        /// </summary>
        /// <param name="b64Image">Base64 data of image</param>
        private TextureMetadata ProcessImage(string b64Image)
        {
            // Get Image Bytes
            byte[] imgBytes = MapUtils.ParseBase64(b64Image);

            // Bytes to FreeImage
            IntPtr texMemory = FreeImage.FreeImage_OpenMemory(
                Marshal.UnsafeAddrOfPinnedArrayElement(imgBytes, 0),
                (uint)imgBytes.Length
            );
            FREE_IMAGE_FORMAT imageFormat = FreeImage.FreeImage_GetFileTypeFromMemory(
                texMemory,
                imgBytes.Length
            );
            IntPtr texHandle = FreeImage.FreeImage_LoadFromMemory(
                imageFormat,
                texMemory,
                0
            );

            // FreeImage to Texture Bytes
            uint texWidth = FreeImage.FreeImage_GetWidth(texHandle);
            uint texHeight = FreeImage.FreeImage_GetHeight(texHandle);
            uint size = texWidth * texHeight * 4;
            byte[] texBytes = new byte[size];
            FreeImage.FreeImage_ConvertToRawBits(
                Marshal.UnsafeAddrOfPinnedArrayElement(texBytes, 0),
                texHandle,
                (int)texWidth * 4,
                32,
                0,
                0,
                0,
                false
            );

            // Release Pointers
            if (texHandle != IntPtr.Zero)
                FreeImage.FreeImage_Unload(texHandle);

            return new TextureMetadata()
            {
                width = (int)texWidth,
                height = (int)texHeight,
                texBytes = texBytes
            };
        }

        private Sprite LoadImage(TextureMetadata texData)
        {
            // Generate Texture
            bool pixelArtMode = LIShipStatus.Instance.CurrentMap.properties.pixelArtMode == true;
            Texture2D texture = new(
                texData.width,
                texData.height,
                TextureFormat.BGRA32,
                1,
                false
            )
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = pixelArtMode ? FilterMode.Point : FilterMode.Bilinear,
            };
            texture.LoadRawTextureData(texData.texBytes);
            texture.Apply(false, true);
            LIShipStatus.Instance.AddMapTexture(texture);

            // Generate Sprite
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100.0f,
                0,
                SpriteMeshType.FullRect
            );

            return sprite;
        }

        private class TextureMetadata
        {
            public int width = 0;
            public int height = 0;
            public byte[] texBytes = Array.Empty<byte>();
        }
    }
}
