using System;
using C7Engine.Pathing;
using C7GameData;
using C7GameData.AIData;
using Serilog;

namespace C7Engine {
	public class SettlerAI : C7GameData.UnitAI {
		private static ILogger log = Log.ForContext<SettlerAI>();
		public SettlerAIData settlerAi;

		public static SettlerAIData MakeAiData(MapUnit unit, Player player) {
			SettlerAIData settlerAiData = new SettlerAIData();
			settlerAiData.goal = SettlerAIData.SettlerGoal.BUILD_CITY;
			//If it's the starting settler, have it settle in place.  Otherwise, use an AI to find a location.
			if (player.cities.Count == 0 && unit.location.cityAtTile == null) {
				settlerAiData.destination = unit.location;
				log.Information("No cities yet!  Set AI for unit to settler AI with destination of " + settlerAiData.destination);
			} else {
				settlerAiData.destination = SettlerLocationAI.findSettlerLocation(unit.location, player);
				if (settlerAiData.destination == Tile.NONE) {
					//This is possible if all tiles within 4 tiles of a city are either not land, or already claimed
					//by another colonist.  Longer-term, the AI shouldn't be building settlers if that is the case,
					//but right now we'll just spike the football to stop the clock and avoid building immediately next to another city.
					settlerAiData.goal = SettlerAIData.SettlerGoal.JOIN_CITY;
					log.Information("Set AI for unit to JOIN_CITY due to lack of locations to settle");
				} else {
					PathingAlgorithm algorithm = PathingAlgorithmChooser.GetAlgorithm(unit);
					settlerAiData.pathToDestination = algorithm.PathFrom(unit.location, settlerAiData.destination);
					log.Information("Set AI for unit to BUILD_CITY with destination of " + settlerAiData.destination);
				}
			}
			return settlerAiData;
		}

		public SettlerAI(SettlerAIData d) {
			settlerAi = d;
		}

		public bool PlayTurn(Player player, MapUnit unit) {
start:
			switch (settlerAi.goal) {
				case SettlerAIData.SettlerGoal.BUILD_CITY:
					if (IsInvalidCityLocation(settlerAi.destination)) {
						log.Information("Seeking new destination for settler " + unit.id + "headed to " + settlerAi.destination);
						//Make sure we're using the new settler AI going forward, including this turn
						settlerAi = MakeAiData(unit, player);
						//Re-process since the unit's goal may have changed.
						//TODO: In theory in the future, it might even have a non-settler AI.  Maybe we should instead return false,
						//and have the PlayerAI re-kick the unit based on a possibly different AI class?
						//Not too worried for settler AI types, but that's a real possibility for other types - an Explorer could
						//very well become a Defender or Attacker if there's no exploration left, for example.
						goto start;
					}
					if (unit.location == settlerAi.destination) {
						log.Information("Building city with " + unit);
						//TODO: This should use a message, and the message handler should cause the disbanding to happen.
						CityInteractions.BuildCity(unit.location.XCoordinate, unit.location.YCoordinate, player.id, unit.owner.GetNextCityName());
						unit.disband();
					} else {
						//If the settler has no destination, then disband rather than crash later.
						if (settlerAi.destination == Tile.NONE) {
							log.Information("Disbanding settler " + unit.id + " with no valid destination");
							unit.disband();
							return false;
						}
						try {
							Tile nextTile = settlerAi.pathToDestination.Next();
							unit.move(unit.location.directionTo(nextTile));
						} catch (Exception ex) {
							//This occurs when on the previous turn, a settler tries to move to the next location on its path, but cannot, due to another
							//civilization's unit (or a barbarian unit) being on that tile.
							//TODO: #213 - If the path cannot be completed, we should create a different path instead.
							//But to do that, the pathing algorithm will need to be enhanced to be aware of when rival units are in the way.
							log.Warning("#213 - Could not get next part of path for unit " + settlerAi + ", " + ex.Message);
						}
					}
					break;
				case SettlerAIData.SettlerGoal.JOIN_CITY:
					if (unit.location.cityAtTile != null) {
						//TODO: Actually join the city.  Haven't added that action.
						//For now, just get rid of the unit.  Sorry, bro.
						unit.disband();
					} else {
						//TODO: Eventually, go to the city we're supposed to join
						//For now, just disband
						unit.disband();
					}
					break;
				default:
					log.Warning("Unknown strategy of " + settlerAi.goal + " for unit");
					break;
			}
			return true;
		}

		private static bool IsInvalidCityLocation(Tile tile) {
			if (tile.cityAtTile != null) {
				Log.ForContext<SettlerAI>().Debug("Cannot build at " + tile + " due to city of " + tile.cityAtTile.name);
				return true;
			}
			foreach (Tile neighbor in tile.neighbors.Values) {
				if (neighbor.cityAtTile != null) {
					Log.ForContext<SettlerAI>().Debug("Cannot build at " + tile + " due to nearby city of " + neighbor.cityAtTile.name);
					return true;
				}
			}
			return false;
		}

		public string SummarizePlan() {
			return "SettlerAI: " + settlerAi.ToString();
		}
	}
}
