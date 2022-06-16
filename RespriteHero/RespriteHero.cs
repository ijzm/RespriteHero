using HarmonyLib;
using MelonLoader;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;
using System;

namespace RespriteHero {

	public class RespriteHero : MelonMod {
		public static string TEXTURE_DIRECTORY = @"\ModConfig\RespriteHero\";

		public override void OnApplicationLateStart() {
			LoggerInstance.Msg("Texture Pack Mod Initialized");
			UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoad;
			string path = Application.dataPath + TEXTURE_DIRECTORY;
			if (!Directory.Exists(path)) {
				LoggerInstance.Msg("ModConfig/RespriteHero folder doesn't exist. Creating one...");
				Directory.CreateDirectory(path);
			}
		}

		public static Sprite LoadPNG(string filePath, Item2 item) {
			Texture2D tex = null;
			byte[] fileData;
			float PPU = 16f;

			if (File.Exists(filePath)) {
				fileData = File.ReadAllBytes(filePath);
				tex = new Texture2D(2, 2);
				tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
			}
			else {
				MelonLogger.Msg("NOT FOUND: " + filePath);
			}

			tex.filterMode = FilterMode.Point;

			List<BoxCollider2D> boxColliders = item.transform.gameObject.GetComponentsInChildren<BoxCollider2D>().ToList<BoxCollider2D>();

			int[] gridNum = new int[2] { 1, 1 };
			gridNum = RespriteHero._BPTGetGridBounds(boxColliders, item);

			if (tex.width > tex.height)
				PPU = tex.width / gridNum[0];
			else
				PPU = tex.height / gridNum[1];

			if (PPU % 2 != 0)
				PPU += 1;
			else if (PPU == 0)
				return null;

			return Sprite.Create(
				tex,
				new Rect(0, 0, tex.width, tex.height),
				new Vector2(0.5f, 0.5f),
				PPU, //Calculated value based on image dimensions and item size
				0,
				SpriteMeshType.FullRect
			);
		}

		public static int[] _BPTGetGridBounds(List<BoxCollider2D> colliders, Item2 item) {
			int[] gridNum = new int[2] { 1, 1 };
			Vector2 max = new Vector2 { x = 0, y = 0 };
			Vector2 min = new Vector2 { x = 0, y = 0 };
			if (colliders.Count > 0) {
				foreach (BoxCollider2D collider in colliders) {
					max.x = collider.size.x/2 + collider.offset.x - item.gameObject.transform.position.x;
					max.y = collider.size.y/2 + collider.offset.y - item.gameObject.transform.position.y;
					min.x = -collider.size.x/2 + collider.offset.x - item.gameObject.transform.position.x;
					min.y = -collider.size.y/2 + collider.offset.y - item.gameObject.transform.position.y;
				}
			}

			gridNum[0] = Mathf.RoundToInt(Mathf.Clamp(Mathf.Abs(max.x) + Mathf.Abs(min.x), 1, Mathf.Infinity));
			gridNum[1] = Mathf.RoundToInt(Mathf.Clamp(Mathf.Abs(max.y) + Mathf.Abs(min.y), 1, Mathf.Infinity));
			
			return gridNum;
		}

		void OnSceneLoad(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode scenemode) {
			if (scene.name == "Game") {
				List<GameObject> prefabs = GameObject.FindObjectOfType<GameManager>().defaultItems;
				List<string> files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + TEXTURE_DIRECTORY).ToList<string>();
				GameObject item = null;
				foreach (string file in files) {
					string[] split0 = file.Split(@"\".ToCharArray());
					string itemName = split0[split0.Length - 1].Replace(".png", "").Trim();
					if (file.Contains("_")) {
						split0 = file.Split(@"\".ToCharArray());
						string[] split1 = split0[split0.Length - 1].Split('_');
						split1[1] = split1[1].Replace(".png", "").Trim();
						itemName = split1[0];
						if (file.Contains("Variant"))
							item = prefabs.Find(x => x.name.Replace("Variant", "").Replace("variant", "").Replace("1", "").Trim() + " Variant" == itemName);
						else
							item = prefabs.Find(x => x.name.Replace("Variant", "").Replace("variant", "").Replace("1", "").Trim() == itemName && !x.name.Contains("Variant Variant"));
						ItemSpriteChanger itemSpriteChanger = item.GetComponent<ItemSpriteChanger>();
						if (itemSpriteChanger != null) {
							List<Sprite> sprites = typeof(ItemSpriteChanger).GetField(
								"sprites",
								System.Reflection.BindingFlags.NonPublic |
								System.Reflection.BindingFlags.Instance
							).GetValue(itemSpriteChanger) as List<Sprite>;

							Sprite newSprite = RespriteHero.LoadPNG(file, item.GetComponent<Item2>());
							if (newSprite != null) {
								int index;
								if (int.TryParse(split1[1], out index)) {
									sprites[index] = newSprite;
									if (index == 0) {
										item.GetComponent<SpriteRenderer>().sprite = newSprite;
										item.GetComponent<SpriteRenderer>().material.mainTexture = newSprite.texture;
									}
								}
							}
						}
					}
					else {
						if (itemName.Contains("Variant")) {
							item = prefabs.Find(x => x.name.Replace("Variant", "").Replace("variant", "").Replace("1", "").Trim() + " Variant" == itemName);
						}
						else {
							item = prefabs.Find(x => x.name.Replace("Variant", "").Replace("variant", "").Replace("1", "").Trim() == itemName && !x.name.Contains("Variant Variant"));
						}
						if (item != null) {
							Sprite newSprite = LoadPNG(file, item.GetComponent<Item2>());
							if (newSprite != null) {
								item.GetComponent<SpriteRenderer>().sprite = newSprite;
								item.GetComponent<SpriteRenderer>().material.mainTexture = newSprite.texture;
							}
						}
					}
				}
			}
		}
	}
}
