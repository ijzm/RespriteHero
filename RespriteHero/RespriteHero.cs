using HarmonyLib;
using MelonLoader;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RespriteHero {

	public class RespriteHero : MelonMod {
		public static string TEXTURE_DIRECTORY = "/../ModConfig/RespriteHero/";
		public override void OnApplicationLateStart() {
			LoggerInstance.Msg("Texture Pack Mod Initialized");

			string path = Application.dataPath + TEXTURE_DIRECTORY;
			if (!Directory.Exists(path)) {
				LoggerInstance.Msg("ModConfig/RespriteHero folder doesn't exist. Creating one...");
				Directory.CreateDirectory(path);
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

	//Every time an item is instantiated. Checks if the texture exists
	//in the given path. If it does, creates the sprite and replaces
	//the current sprite.
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
					if (File.Exists(filename)) {
						Sprite newSprite = RespriteHero.LoadPNG(filename);
						sprites[i] = newSprite;
					}
				}

				return;
			}

			filename = Application.dataPath + RespriteHero.TEXTURE_DIRECTORY + name + ".png";

			if (File.Exists(filename)) {
				MelonLogger.Msg($"[SpriteRenderer]: Opening filename[{filename}]");
				Sprite newSprite = RespriteHero.LoadPNG(filename);
				SpriteRenderer spriteRenderer = __instance.gameObject.GetComponent<SpriteRenderer>();
				spriteRenderer.sprite = newSprite;
			}
		}
	}
}
