using C7GameData;
using Xunit;

namespace C7GameDataTests;

public class GameMapTest
{
	[Fact]
	public void DefaultGameMap_ShouldGenerateGameMap80TilesTall()
	{
		GameMap gm = GameMap.Generate(new GameData());
		Assert.Equal(80, gm.numTilesTall);
	}
}
