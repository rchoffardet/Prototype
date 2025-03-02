using System;
using System.Collections.Generic;
using System.Linq;

namespace C7GameData {
	public class TileKnowledge {
		HashSet<Tile> knownTiles = new HashSet<Tile>();
		HashSet<Tile> borderTiles = new HashSet<Tile>();
		HashSet<Tile> visibleTiles = new HashSet<Tile>();
		private readonly List<City> cities;
		private readonly List<MapUnit> units;
		private readonly int height;
		private readonly int width;
		private readonly byte[] cityVisibilityBitmap;
		private readonly byte[] unitVisibilityBitmap;
		private readonly GameMap map;
		private byte[] visibilityBitmap;

		public TileKnowledge(List<City> cities, List<MapUnit> units, GameMap map) {
			this.cities = cities;
			this.units = units;
			this.map = map;

			var numberOfTiles = map.numTilesTall * map.numTilesWide;
			var numberOfBytes = numberOfTiles / 8 + (numberOfTiles % 8 > 0 ? 1 : 0);
			cityVisibilityBitmap = new byte[numberOfBytes];
			unitVisibilityBitmap = new byte[numberOfBytes];
		}

		public bool isTileKnown(Tile t) {
			return knownTiles.Contains(t);
		}

		public bool isTileVisible(Tile t) {
			var index = GetBitIndex(t);
			byte mask = (byte)(1 << (index % 8));
			return (visibilityBitmap[index / 8] & mask) == mask;
		}

		public bool isBorderOfTileKnowlege(Tile t) {
			return borderTiles.Contains(t);
		}

		/**
		 * Returns a copy of the list of known tiles.
		 * This prevents external modifications.
		 **/
		public List<Tile> AllKnownTiles() {
			List<Tile> list = new List<Tile>();
			foreach (Tile t in knownTiles) {
				list.Add(t);
			}
			return list;
		}

		public void AddVisibilityTo(Tile unitLocation) {
			knownTiles.Add(unitLocation);
			borderTiles.Remove(unitLocation);

			foreach (Tile t in unitLocation.neighbors.Values) {
				knownTiles.Add(t);
				SetNeighborsAsKnown(t);
			}
		}

		// neighboring tiles should not be added when loading tile knowledge
		// from a .sav file
		internal bool AddKnowledgeOf(Tile tile) {
			bool added = knownTiles.Add(tile);
			return added;
		}

		public void Compute() {
			Array.Clear(cityVisibilityBitmap, 0, cityVisibilityBitmap.Length);
			foreach (var city in cities) {
				foreach (var tile in city.GetTilesWithinBorders()) {
					var index = GetBitIndex(tile);
					cityVisibilityBitmap[index / 8] |= (byte)(1 << (index % 8));
				}
			}

			Array.Clear(unitVisibilityBitmap, 0, unitVisibilityBitmap.Length);
			foreach (var unit in units) {
				foreach (var tile in GetTilesAround(unit.location).Append(unit.location)) {
					var index = GetBitIndex(tile);
					unitVisibilityBitmap[index / 8] |= (byte)(1 << (index % 8));
				}
			}

			var result = new byte[cityVisibilityBitmap.Length];
			for (int i = 0; i < cityVisibilityBitmap.Length; i++) {
				result[i] = (byte)(cityVisibilityBitmap[i] | unitVisibilityBitmap[i]);
			}

			visibilityBitmap = result;
		}

		private void SetNeighborsAsKnown(Tile tile) {
			foreach (Tile neighbor in tile.neighbors.Values) {
				knownTiles.Add(neighbor);
				borderTiles.Remove(neighbor);

				SetNeighborsAsBorderIfNotKnownAlready(neighbor);
			}
		}

		private void SetNeighborsAsBorderIfNotKnownAlready(Tile tile) {
			borderTiles.Remove(tile);
			foreach (Tile border in tile.neighbors.Values) {
				if (!knownTiles.Contains(border)) {
					borderTiles.Add(border);
				}
			}
		}

		private int GetBitIndex(Tile tile) {
			return map.tileCoordsToIndex(map.wrapTileX(tile.XCoordinate), map.wrapTileY(tile.YCoordinate));
		}

		private IEnumerable<Tile> GetTilesAround(Tile tile) {
			yield return map.tileNeighbor(tile, TileDirection.NORTH);
			yield return map.tileNeighbor(tile, TileDirection.NORTHEAST);
			yield return map.tileNeighbor(tile, TileDirection.EAST);
			yield return map.tileNeighbor(tile, TileDirection.SOUTHEAST);
			yield return map.tileNeighbor(tile, TileDirection.SOUTH);
			yield return map.tileNeighbor(tile, TileDirection.SOUTHWEST);
			yield return map.tileNeighbor(tile, TileDirection.WEST);
			yield return map.tileNeighbor(tile, TileDirection.NORTHWEST);
		}
	}
}
