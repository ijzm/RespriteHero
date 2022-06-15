using HarmonyLib;
using MelonLoader;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;

namespace RespriteHero {

	public class RespriteHero : MelonMod {
		public static string TEXTURE_DIRECTORY = "/../ModConfig/RespriteHero/";
		//Cache of all the used sprites
		public static Dictionary<string, Sprite> SPRITE_CACHE = new Dictionary<string, Sprite>();
		//Cache of all unused sprites, so we don't have to call
		//IO operations every time we want to use a sprite
		public static List<string> UNUSED_CACHE = new List<string>();

		public override void OnApplicationLateStart() {
			LoggerInstance.Msg("Texture Pack Mod Initialized");

			string path = Application.dataPath + TEXTURE_DIRECTORY;
			if (!Directory.Exists(path)) {
				LoggerInstance.Msg("ModConfig/RespriteHero folder doesn't exist. Creating one...");
				Directory.CreateDirectory(path);
			}
		}

		public static Sprite LoadPNGCache(string filePath, Item2 item) {
			if (SPRITE_CACHE.ContainsKey(filePath)) {
				return SPRITE_CACHE[filePath];
			}
			else {
				Sprite sprite = LoadPNG(filePath, item);
				SPRITE_CACHE.Add(filePath, sprite);
				return sprite;
			}
		}

		public static Sprite LoadPNG(string filePath, Item2 item) {
			Texture2D tex = null;
			byte[] fileData;
			MelonLogger.Msg(item.gameObject);
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

		public override void OnApplicationStart() {
			List<Item2> prefabs = Resources.FindObjectsOfTypeAll<Item2>().ToList<Item2>();
			foreach (Item2 item2 in prefabs) {
				GameObject item = item2.gameObject;
				string baseName = item.name.Replace("(Clone)", "").Trim();
				string filename = Application.dataPath + RespriteHero.TEXTURE_DIRECTORY + baseName.Replace("Variant", "").Replace("variant", "").Replace("1", "").Trim() + ".png";
				if (File.Exists(filename)) {
					SpriteRenderer prefabRenderer = item.GetComponent<SpriteRenderer>();
					if (RespriteHero.SPRITE_CACHE.ContainsKey(filename) || File.Exists(filename)) {
						MelonLogger.Msg($"[SpriteRenderer]: Opening filename[{filename}]");
						Sprite newSprite = RespriteHero.LoadPNGCache(filename, item2);
						if (newSprite != null) {
							prefabRenderer.sprite = newSprite;
							prefabRenderer.material.mainTexture = newSprite.texture;
						}
						else {
							MelonLogger.Msg("Sprite cannot be loaded");
						}
					}
					else {
						RespriteHero.UNUSED_CACHE.Add(filename);
					}
				}
			}
			base.OnApplicationStart();
		}
	}
}
