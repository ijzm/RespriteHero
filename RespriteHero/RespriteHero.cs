using HarmonyLib;
using MelonLoader;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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

		public static Sprite LoadPNGCache(string filePath) {
			if (SPRITE_CACHE.ContainsKey(filePath)) {
				return SPRITE_CACHE[filePath];
			}
			else {
				Sprite sprite = LoadPNG(filePath);
				SPRITE_CACHE.Add(filePath, sprite);
				return sprite;
			}
		}

		public static Sprite LoadPNG(string filePath) {
			Texture2D tex = null;
			byte[] fileData;

			if (File.Exists(filePath)) {
				fileData = File.ReadAllBytes(filePath);
				tex = new Texture2D(2, 2);
				tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
			}
			else {
				MelonLogger.Msg("NOT FOUND: " + filePath);
			}

			tex.filterMode = FilterMode.Point;

			return Sprite.Create(
				tex,
				new Rect(0, 0, tex.width, tex.height),
				new Vector2(0.5f, 0.5f),
				16f //Same value as Backpack Hero PPU
			);
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
						Sprite newSprite = RespriteHero.LoadPNGCache(filename);
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
				Sprite newSprite = RespriteHero.LoadPNGCache(filename);
				SpriteRenderer spriteRenderer = __instance.gameObject.GetComponent<SpriteRenderer>();
				spriteRenderer.sprite = newSprite;
			}
			else {
				RespriteHero.UNUSED_CACHE.Add(filename);
			}
		}
	}
}
