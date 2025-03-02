using System.Net.Mime;
using C7GameData;
using ConvertCiv3Media;
using Godot;

namespace C7.Map {
	public partial class FogOfWarLayer : LooseLayer {

		private readonly ImageTexture fogOfWarTexture;
		private readonly Vector2 tileSize;
		private readonly Vector2 fogOfwarSize;
		private readonly ImageTexture pollutionTexture;
		private readonly Vector2 pollutionSpriteSize;
		private readonly Font font;

		public FogOfWarLayer() {
			Pcx fogOfWarPcx = new Pcx(Util.Civ3MediaPath("Art/Terrain/FogOfWar.pcx"));
			fogOfWarTexture = PCXToGodot.getPureAlphaFromPCX(fogOfWarPcx);
			fogOfwarSize = fogOfWarTexture.GetSize() / 9;
			tileSize = new Vector2(128, 64);

			pollutionTexture = Util.LoadTextureFromPCX("Art/Terrain/pollution.pcx");
			pollutionSpriteSize = new Vector2(128, 64);

			font = ResourceLoader.Load<FontFile>("res://Fonts/NotoSans-Regular.ttf", null, ResourceLoader.CacheMode.Ignore);
		}

		public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter) {
			Vector2 screenTarget = new (tileCenter.X-64, tileCenter.Y-32);
			TileKnowledge tileKnowledge = gameData.GetHumanPlayers()[0].tileKnowledge;

			//N.B. FogOfWar.pcx handles both totally unknown and fogged tiles, indexed in the same file.
			//Hence the trinary math rather than the more commonplace binary.

			var visibilityFlag = tileKnowledge.isTileVisible(tile) ? 1 : 0;
			var knownFlag = tileKnowledge.isTileKnown(tile) ? 1 : 0;

			looseView.DrawString(font, new(tileCenter.X - tileSize.X/2, tileCenter.Y - tileSize.Y/16), $"[{visibilityFlag}, {knownFlag}]", HorizontalAlignment.Center, tileSize.X);

			var column = 0;
			var row = 0;

			var nw = tile.neighbors[TileDirection.NORTHWEST];
			var n = tile.neighbors[TileDirection.NORTH];
			var ne = tile.neighbors[TileDirection.NORTHEAST];
			var w = tile.neighbors[TileDirection.WEST];
			var e = tile.neighbors[TileDirection.EAST];
			var sw = tile.neighbors[TileDirection.SOUTHWEST];
			var s = tile.neighbors[TileDirection.SOUTH];
			var se = tile.neighbors[TileDirection.SOUTHEAST];

			// if (!tileKnowledge.isTileKnown(tile)) {
			// 	DrawFog(looseView, screenTarget, 0, 0);
			// 	return;
			// }

			// if (tileKnowledge.isTileKnown(tile) && !tileKnowledge.isTileVisible(tile)) {
			// 	if (tileKnowledge.isTileKnown(ne) || tileKnowledge.isTileKnown(n) || tileKnowledge.isTileKnown(nw)) {
			// 		column += 1;
			// 	}
			//
			// 	if (tileKnowledge.isTileKnown(nw) || tileKnowledge.isTileKnown(w) || tileKnowledge.isTileKnown(sw)) {
			// 		column += 3;
			// 	}
			//
			// 	if (tileKnowledge.isTileKnown(ne) || tileKnowledge.isTileKnown(e) || tileKnowledge.isTileKnown(se)) {
			// 		row += 1;
			// 	}
			//
			// 	if (tileKnowledge.isTileKnown(se) || tileKnowledge.isTileKnown(s) || tileKnowledge.isTileKnown(sw)) {
			// 		row += 3;
			// 	}
			//
			// 	DrawFog(looseView, screenTarget, column, row);
			// 	return;
			// }

			if (tileKnowledge.isTileVisible(ne) || tileKnowledge.isTileVisible(n) || tileKnowledge.isTileVisible(nw)) {
				column += 2;
			} else if (tileKnowledge.isTileKnown(ne) || tileKnowledge.isTileKnown(n) || tileKnowledge.isTileKnown(nw)) {
				column += 1;
			}

			if (tileKnowledge.isTileVisible(nw) || tileKnowledge.isTileVisible(w) || tileKnowledge.isTileVisible(sw)) {
				column += 6;
			} else if (tileKnowledge.isTileKnown(nw) || tileKnowledge.isTileKnown(w) || tileKnowledge.isTileKnown(sw)) {
				column += 3;
			}

			if(tileKnowledge.isTileVisible(ne) || tileKnowledge.isTileVisible(e) || tileKnowledge.isTileVisible(se)) {
				row += 2;
			} else if (tileKnowledge.isTileKnown(ne) || tileKnowledge.isTileKnown(e) || tileKnowledge.isTileKnown(se)) {
				row += 1;
			}

			if(tileKnowledge.isTileVisible(se) || tileKnowledge.isTileKnown(s) || tileKnowledge.isTileVisible(sw)) {
				row += 6;
			} else if (tileKnowledge.isTileKnown(se) || tileKnowledge.isTileKnown(s) || tileKnowledge.isTileKnown(sw)) {
				row += 3;
			}

			// if(column == 0 && row == 0) {
			// 	//do nothing if the tile is totally unknown
			// 	return;
			// }
			// if(column == 8 && row == 8) {
			// 	//do nothing if the tile is totally known
			// 	return;
			// }

			//DrawPollution(looseView, tileCenter);
			DrawFog(looseView, screenTarget, column, row);
			looseView.DrawString(font, new(tileCenter.X-tileSize.X/2, tileCenter.Y + tileSize.Y/4), $"[{column}, {row}]", HorizontalAlignment.Center, tileSize.X);


			// if ((tileKnowledge.isTileKnown(tile) || tileKnowledge.isBorderOfTileKnowlege(tile))&& !tileKnowledge.isTileVisible(tile)) {
			// 	DrawPollution(looseView, new(tileCenter.X - tileSize.X/2, tileCenter.Y - tileSize.Y/2));
			// }
			//do nothing if the tile is known (equiv to the lower-right)
		}

		private void DrawPollution(LooseView looseView, Vector2 tileCenter) {
			looseView.DrawTextureRectRegion(
				pollutionTexture,
				new Rect2(tileCenter, tileSize),
				new Rect2(4 * pollutionSpriteSize.X, 4 * pollutionSpriteSize.Y, pollutionSpriteSize)
			);
		}

		private void DrawFog(LooseView looseView, Vector2 tileCenter, int column, int row) {
			looseView.DrawTextureRectRegion(
				fogOfWarTexture,
				new Rect2(tileCenter, new Vector2(128, 64)),
				new Rect2(column * fogOfwarSize.X, row * fogOfwarSize.Y, fogOfwarSize*0.999f)
			);
		}
	}
}
