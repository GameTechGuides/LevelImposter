using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LevelImposter.Core;
using BepInEx.IL2CPP.Utils.Collections;

namespace LevelImposter.Shop
{
    public class MapBanner : MonoBehaviour
    {
        public LIMetadata map;
        public GameObject loadingSpinner;
        public Image thumbnail;
        public TMPro.TMP_Text nameText;
        public TMPro.TMP_Text authorText;
        public TMPro.TMP_Text descText;
        public Button downloadButton;
        public Button playButton;
        public Button deleteButton;

        public MapBanner(IntPtr intPtr) : base(intPtr)
        {
        }

        private void Awake()
        {
            loadingSpinner = transform.FindChild("LoadOverlay").gameObject;
            thumbnail = transform.FindChild("Thumbnail").GetComponent<Image>();
            nameText = transform.FindChild("Title").GetComponent<TMPro.TMP_Text>();
            authorText = transform.FindChild("Author").GetComponent<TMPro.TMP_Text>();
            descText = transform.FindChild("Description").GetComponent<TMPro.TMP_Text>();
            downloadButton = transform.FindChild("DownloadBtn").GetComponent<Button>();
            playButton = transform.FindChild("PlayBtn").GetComponent<Button>();
            deleteButton = transform.FindChild("DeleteBtn").GetComponent<Button>();
        }

        private void Start()
        {
            downloadButton.onClick.AddListener((Action)OnDownload);
            playButton.onClick.AddListener((Action)OnPlay);
            deleteButton.onClick.AddListener((Action)OnDelete);
            UpdateButtons();
        }

        public void SetMap(LIMetadata map)
        {
            this.map = map;
            loadingSpinner.SetActive(false);
            nameText.text = map.name;
            authorText.text = map.authorName;
            descText.text = map.description;
            UpdateButtons();
            StartCoroutine(CoGetThumbnail().WrapToIl2Cpp());
        }

        public void UpdateButtons()
        {
            if (map == null)
            {
                downloadButton.interactable = false;
                playButton.interactable = false;
                deleteButton.interactable = false;
            }
            else
            {
                bool mapExists = MapFileAPI.Instance.Exists(map.id);
                bool isOnline = !string.IsNullOrEmpty(map.authorID);
                downloadButton.interactable = !mapExists && isOnline;
                playButton.interactable = mapExists;
                deleteButton.interactable = mapExists && isOnline;
            }
        }

        public void OnDownload()
        {
            downloadButton.interactable = false;
            loadingSpinner.SetActive(true);
            LevelImposterAPI.Instance.DownloadMap(new System.Guid(map.id), (LIMap map) =>
            {
                MapFileAPI.Instance.Save(map);
                loadingSpinner.SetActive(false);
                UpdateButtons();
            });
        }

        public void OnPlay()
        {
            ShopManager.Instance.LaunchMap(map.id);
        }

        public void OnDelete()
        {
            MapFileAPI.Instance.Delete(map.id);
            ThumbnailFileAPI.Instance.Delete(map.id);
            UpdateButtons();
        }

        public IEnumerator CoGetThumbnail()
        {
            if (string.IsNullOrEmpty(map.thumbnailURL))
                yield break;
            if (ThumbnailFileAPI.Instance.Exists(map.id))
            {
                ThumbnailFileAPI.Instance.Get(map.id, (Texture2D texture) =>
                {
                    thumbnail.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
                });
            }
            else
            {
                LevelImposterAPI.Instance.DownloadThumbnail(map, (Texture2D texture) =>
                {
                    byte[] textureData = texture.EncodeToPNG();
                    ThumbnailFileAPI.Instance.Save(map.id, textureData);
                    thumbnail.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f);
                });
            }
        }
    }
}