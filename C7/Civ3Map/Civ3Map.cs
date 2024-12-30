using Godot;
using System.Collections;
using System.Collections.Generic;
using ConvertCiv3Media;
using C7GameData;

public partial class Civ3Map : Node2D
{
	public List<Tile> Civ3Tiles;
	public int[,] Map { get; protected set; }
	TileMap TM;
	public TileSet TS { get; protected set; }
	private int[,] TileIDLookup;
	// NOTE: The following two must be set externally before running TerrainAsTileMap
	public int MapWidth;
	public int MapHeight;
	// If a mod is in effect, set this, otherwise set to "" or "Conquests"
	public string ModRelPath = "";
	public Civ3Map(int mapWidth, int mapHeight, string modRelPath = "")
	{
		MapWidth = mapWidth;
		MapHeight = mapHeight;
		ModRelPath = modRelPath;
	}
	public void TerrainAsTileMap() {
		if (TM != null) { RemoveChild(TM); }
		// Although tiles appear isometric, they are logically laid out as a checkerboard pattern on a square grid
		TM = new TileMap();
		TM.TileSet.TileSize = new Vector2I(64,32);
		// TM.CenteredTextures = true;
		TS = TM.TileSet;

		TileIDLookup = new int[9,81];

		// int id = TS.GetLastUnusedTileId();

		// Make blank default tile
		// TODO: Make red tile or similar
		// NOTE: Need an unused tile at 0, anyway, to test to see if real tile has been loaded yet

		// TS.CreateTile(id);
		// id++;

		Map = new int[MapWidth,MapHeight];

		// Populate map values
		if(Civ3Tiles != null)
		{
			foreach (Tile tile in Civ3Tiles)
			{
				Map[tile.xCoordinate, tile.yCoordinate] = 0;
				Map[tile.xCoordinate, tile.yCoordinate] = TileIDLookup[tile.ExtraInfo.BaseTerrainFileID,tile.ExtraInfo.BaseTerrainImageID];
			}
		}
	}
}
