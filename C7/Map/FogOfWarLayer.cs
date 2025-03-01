using C7GameData;
using ConvertCiv3Media;
using Godot;

namespace C7.Map {
	public partial class FogOfWarLayer : LooseLayer {

		private readonly ImageTexture fogOfWarTexture;
		private readonly Vector2 tileSize;
		private readonly ImageTexture pollutionPcx;
		private readonly Vector2 pollutionSpriteSize;

		public FogOfWarLayer() {
			Pcx fogOfWarPcx = new Pcx(Util.Civ3MediaPath("Art/Terrain/FogOfWar.pcx"));
			fogOfWarTexture = PCXToGodot.getPureAlphaFromPCX(fogOfWarPcx);
			tileSize = fogOfWarTexture.GetSize() / 9;

			pollutionPcx = Util.LoadTextureFromPCX("Art/Terrain/pollution.pcx");
			pollutionSpriteSize = new Vector2(128, 64);
		}

		public override void drawObject(LooseView looseView, GameData gameData, Tile tile, Vector2 tileCenter) {
			Vector2 fogOrigin = new(tileCenter.X - tileSize.X/2, tileCenter.Y);
			Rect2 screenTarget = new Rect2(fogOrigin, tileSize);
			TileKnowledge tileKnowledge = gameData.GetHumanPlayers()[0].tileKnowledge;

			DrawPollution(looseView, new Vector2(0, 0));
			//N.B. FogOfWar.pcx handles both totally unknown and fogged tiles, indexed in the same file.
			//Hence the trinary math rather than the more commonplace binary.

			if (!tileKnowledge.isTileKnown(tile) || tileKnowledge.isBorderOfTileKnowlege(tile)) {
				int sum = 0;
				if (tileKnowledge.isTileKnown(tile.neighbors[TileDirection.NORTH]) || tileKnowledge.isTileKnown(tile.neighbors[TileDirection.NORTHWEST]) || tileKnowledge.isTileKnown(tile.neighbors[TileDirection.NORTHEAST]))
					sum += 2;
				if (tileKnowledge.isTileKnown(tile.neighbors[TileDirection.WEST]) || tileKnowledge.isTileKnown(tile.neighbors[TileDirection.NORTHWEST]) || tileKnowledge.isTileKnown(tile.neighbors[TileDirection.SOUTHWEST]))
					sum += 6;
				if (tileKnowledge.isTileKnown(tile.neighbors[TileDirection.EAST]) || tileKnowledge.isTileKnown(tile.neighbors[TileDirection.NORTHEAST]) || tileKnowledge.isTileKnown(tile.neighbors[TileDirection.SOUTHEAST]))
					sum += 18;
				if (tileKnowledge.isTileKnown(tile.neighbors[TileDirection.SOUTH]) || tileKnowledge.isTileKnown(tile.neighbors[TileDirection.SOUTHWEST]) || tileKnowledge.isTileKnown(tile.neighbors[TileDirection.SOUTHEAST]))
					sum += 54;

				looseView.DrawTextureRectRegion(fogOfWarTexture, screenTarget, getRect(sum));
			}

			if ((tileKnowledge.isTileKnown(tile) || tileKnowledge.isBorderOfTileKnowlege(tile))&& !tileKnowledge.isTileVisible(tile)) {
				DrawPollution(looseView, new(tileCenter.X - tileSize.X/2, tileCenter.Y - tileSize.Y/2));
			}
			//do nothing if the tile is known (equiv to the lower-right)
		}

		private Rect2 getRect(int sum) {
			int row = sum / 9;
			int col = sum % 9;
			// The 0.999f scaling is a hack to prevent "seams".
			return new Rect2(col * tileSize.X, row * tileSize.Y, tileSize * 0.999f);
		}

		private void DrawPollution(LooseView looseView, Vector2 tileCenter) {
			looseView.DrawTextureRectRegion(
				pollutionPcx,
				new Rect2(tileCenter, tileSize),
				new Rect2(4 * pollutionSpriteSize.X, 4 * pollutionSpriteSize.Y, pollutionSpriteSize)
			);
		}
	}
}
