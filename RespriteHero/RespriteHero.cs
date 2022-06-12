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

			return Sprite.Create(
				tex,
				new Rect(0, 0, tex.width, tex.height),
				new Vector2(0.5f, 0.5f),
				PPU, //Same value as Backpack Hero PPU
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
					max.x = collider.bounds.max.x + collider.offset.x - item.gameObject.transform.position.x;
					max.y = collider.bounds.max.y + collider.offset.y - item.gameObject.transform.position.y;
					min.x = collider.bounds.min.x + collider.offset.x - item.gameObject.transform.position.x;
					min.y = collider.bounds.min.y + collider.offset.y - item.gameObject.transform.position.y;
				}
			}
			gridNum[0] = Mathf.RoundToInt(Mathf.Abs(max.x) + Mathf.Abs(min.x));
			gridNum[1] = Mathf.RoundToInt(Mathf.Abs(max.y) + Mathf.Abs(min.y));

			return gridNum;
		}
	}

	//Every time an item is instantiated:
	//Checks if the sprite exists in the cache
	//If not, check if the expr
	//This is not very efficient. But performance doesn't matter in
	//the current context
	[HarmonyPatch(typeof(Item2), "Start")]
	class Item2_Start_Patch {
		static void Postfix(ref Item2 __instance) {
			string name = __instance.gameObject.name;
			name = name.Replace("(Clone)", "");
			name = name.Replace("Variant", "");
			name = name.Replace("variant", "");
			name = name.Trim();
			string filename;

			ItemSpriteChanger itemSpriteChanger = __instance.GetComponent<ItemSpriteChanger>();
			if (itemSpriteChanger != null) {
				List<Sprite> sprites = typeof(ItemSpriteChanger).GetField(
					"sprites",
					System.Reflection.BindingFlags.NonPublic |
					System.Reflection.BindingFlags.Instance
				).GetValue(itemSpriteChanger) as List<Sprite>;

				for (int i = 0; i < sprites.Count; i++) {
					filename = Application.dataPath + RespriteHero.TEXTURE_DIRECTORY + name + "_" + i + ".png";
					MelonLogger.Msg($"[ItemSpriteChanger]: Opening filename[{filename}]");

					if (RespriteHero.SPRITE_CACHE.ContainsKey(filename) || File.Exists(filename)) {
						Sprite newSprite = RespriteHero.LoadPNGCache(filename, __instance);
						sprites[i] = newSprite;
					}
					else {
						RespriteHero.UNUSED_CACHE.Add(filename);
					}
				}

				return;
			}

			filename = Application.dataPath + RespriteHero.TEXTURE_DIRECTORY + name + ".png";

			if (RespriteHero.SPRITE_CACHE.ContainsKey(filename) || File.Exists(filename)) {
				MelonLogger.Msg($"[SpriteRenderer]: Opening filename[{filename}]");
				Sprite newSprite = RespriteHero.LoadPNGCache(filename, __instance);
				SpriteRenderer spriteRenderer = __instance.gameObject.GetComponent<SpriteRenderer>();
				spriteRenderer.sprite = newSprite;
			}
			else {
				RespriteHero.UNUSED_CACHE.Add(filename);
			}
		}
	}
}
