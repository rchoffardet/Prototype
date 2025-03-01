using System;
using System.Collections.Generic;
using System.Linq;
using C7Engine.Pathing;
using C7GameData;
using C7GameData.AIData;
using C7Engine.AI;
using C7Engine.AI.StrategicAI;
using C7Engine.AI.UnitAI;
using Serilog;

namespace C7Engine {
	public class PlayerAI {
		private static ILogger log = Log.ForContext<PlayerAI>();

		public static void PlayTurn(Player player, Random rng, List<Tech> techs) {
			if (player.isHuman || player.isBarbarians) {
				return;
			}
			log.Information("-> Begin " + player.civilization.cityNames[0] + " turn");

			if (player.turnsUntilPriorityReevaluation == 0) {
				log.Information("Re-evaluating strategic priorities for " + player);
				List<StrategicPriority> priorities = StrategicPriorityArbitrator.Arbitrate(player);
				player.strategicPriorityData.Clear();
				foreach (StrategicPriority priority in priorities) {
					player.strategicPriorityData.Add(priority);
				}
				player.turnsUntilPriorityReevaluation = 15 + GameData.rng.Next(10);
				log.Information(player.turnsUntilPriorityReevaluation + " turns until next re-evaluation");
			} else {
				player.turnsUntilPriorityReevaluation--;
			}

			if (player.currentlyResearchedTech == null) {
				Tech toResearch = PickTechToResearch(player, techs);
				if (toResearch == null) {
					log.Information($"Player {player.civilization.name} has no techs available to research.");
					player.SetCurrentlyResearchedTech(null);
				} else {
					log.Information($"Player {player.civilization.name} is researching {toResearch.Name}.");
					player.SetCurrentlyResearchedTech(toResearch.id);
				}
			}

			//Do things with units.  Copy into an array first to avoid collection-was-modified exception
			foreach (MapUnit unit in player.units.ToArray()) {
				//For each unit, if there's already an AI task assigned, it will attempt to complete its goal.
				//It may fail due to conditions having changed since that goal was assigned; in that case it will
				//get a new task to try to complete.

				bool unitDone = false;
				int attempts = 0;
				int maxAttempts = 2;    //safety valve so we don't freeze the UI if SetAIForUnit returns something that fails
				while (!unitDone) {
					if (unit.currentAI == null || attempts > 0) {
						unit.currentAI = GetAIForUnit(unit, player);
					}

					unitDone = unit.currentAI.PlayTurn(player, unit);
					attempts++;
					if (!unitDone && attempts >= maxAttempts) {
						log.Warning($"Hit max AI attempts of {maxAttempts} for unit {unit} at {unit.location} without succeeding.  This indicates GetAIForUnit returned an impossible task, and should be debugged.");
						break;
					}
				}

				player.tileKnowledge.AddVisibilityTo(unit.location);
			}
		}

		public static UnitAI GetAIForUnit(MapUnit unit, Player player) {
			//figure out an AI behavior
			//TODO: Use strategies, not names
			if (unit.unitType.name == "Settler") {
				return new SettlerAI(SettlerAI.MakeAiData(unit, player));
			} else if (unit.unitType.name == "Worker") {
				return new WorkerAI(WorkerAI.MakeAiData(unit, player));
			} else if (unit.location.cityAtTile != null && unit.location.unitsOnTile.Count(u => u.unitType.defense > 0 && u != unit) == 0) {
				return new DefenderAI(DefenderAI.MakeAiData(unit, player));
			} else if (GetCombatAIIfUnitCanAttackNearbyBarbCamp(unit, player) is UnitAI unitAI && unitAI != null) {
				log.Information("Set unit " + unit + " to take out barb camp");
				return unitAI;
			} else if (unit.unitType.name == "Catapult") {
				//For now tell catapults to sit tight.  It's getting really annoying watching them pointlessly bombard barb camps forever
				return new DefenderAI(DefenderAI.MakeAiData(unit, player));
			} else {
				ExplorerAIData? maybeAiData = ExplorerAI.MaybeMakeAiData(unit, player);
				if (maybeAiData != null) {
					return new ExplorerAI(maybeAiData);
				}

				//Nowhere to explore.  What to do now?
				//Priority 1: Adequate defense of cities.
				//Future Priority 1: Escorting Settlers
				//Priority 2: Clearing out barbs
				//Priority 3: Defending chokepoints
				//Priority 4: ???
				//Priority 5: Profit!
				//(Realistically, as we evolve there will be a lot of options, such as defending borders from barbs, preparing attackers on other civs, defending
				//resources.  I expect we'll have some sort of arbiter that decides between competing priorities, with each being given a score as to how important
				//they are, including a weight by how far away the task is.  But this will evolve gradually over a long time)

				//As of today (4/7/2022), let's tackle just one of those - adequate defense of cities.  The AI is really good at losing cities to barbs right now,
				//and that's a problem.

				City nearestCityToDefend = FindNearbyCityToDefend(unit, player);

				DefenderAIData newUnitAIData = new DefenderAIData();
				newUnitAIData.destination = nearestCityToDefend.location;
				newUnitAIData.goal = DefenderAIData.DefenderGoal.DEFEND_CITY;

				PathingAlgorithm algorithm = PathingAlgorithmChooser.GetAlgorithm(unit);
				newUnitAIData.pathToDestination = algorithm.PathFrom(unit.location, newUnitAIData.destination);

				log.Information($"Unit {unit} tasked with defending {nearestCityToDefend.name}");
				return new DefenderAI(newUnitAIData);
			}
		}

		private static UnitAI GetCombatAIIfUnitCanAttackNearbyBarbCamp(MapUnit unit, Player player) {
			if (unit.unitType.attack <= 0) {
				return null;
			}

			List<Tile> reachableBarbCampsTiles = player.tileKnowledge.AllKnownTiles()
				.Where(t => unit.CanEnterTile(t, true) && t.hasBarbarianCamp).ToList();

			Tile closestBarbCamp = Tile.NONE;
			int closestBarbDistance = int.MaxValue;
			foreach (Tile t in reachableBarbCampsTiles) {
				int crowDistance = t.distanceTo(unit.location);
				if (crowDistance < closestBarbDistance) {
					closestBarbCamp = t;
					closestBarbDistance = crowDistance;
				}
			}

			if (closestBarbDistance <= 3) {
				CombatAIData caid = new CombatAIData();

				PathingAlgorithm algorithm = PathingAlgorithmChooser.GetAlgorithm(unit);
				caid.path = algorithm.PathFrom(unit.location, closestBarbCamp);
				return new CombatAI(caid);
			}
			return null;
		}

		/**
		 * Finds a nearby city that could use extra defenders.  Currently, that is a city that is tied
		 * for the fewest units present, and among those, it's the closest.
		 *
		 * This is not a brilliant method, with many flaws such as not considering units already en route to defend,
		 * whether the city needs more defenders, or if the units present are defenders.
		 *
		 * However, in the spirit of incrementalism, sending units to defend is still better than not sending them to defend.
		 */
		private static City FindNearbyCityToDefend(MapUnit unit, Player player) {
			int minDefenders = int.MaxValue;
			//TODO: Just being there doesn't mean a unit is a defender.
			List<City> citiesWithFewestDefenders = new List<City>();
			foreach (City c in player.cities) {
				if (c.location.unitsOnTile.Count < minDefenders) {
					minDefenders = c.location.unitsOnTile.Count;
					citiesWithFewestDefenders.Clear();
					citiesWithFewestDefenders.Add(c);
				}
			}
			City nearestCityToDefend = City.NONE;
			int closestCityDistance = int.MaxValue;
			foreach (City c in citiesWithFewestDefenders) {
				int distanceToCity = c.location.distanceTo(unit.location);
				if (distanceToCity < closestCityDistance) {
					nearestCityToDefend = c;
					closestCityDistance = distanceToCity;
				}
			}
			return nearestCityToDefend;
		}

		private static Tech PickTechToResearch(Player player, List<Tech> techs) {
			List<Tech> possibleTechs = new();

			// Figure out what techs we're allowed to research.
			foreach (Tech tech in techs) {
				if (tech.EraCivilopediaName != player.eraCivilopediaName) {
					continue;
				}
				bool prereqsKnown = true;
				foreach (Tech prereq in tech.Prerequisites) {
					if (!player.knownTechs.Contains(prereq.id)) {
						prereqsKnown = false;
						break;
					}
				}
				if (!prereqsKnown) {
					continue;
				}
				possibleTechs.Add(tech);
			}

			if (possibleTechs.Count == 0) {
				return null;
			}

			return possibleTechs[(int)GameData.rng.NextInt64(possibleTechs.Count)];
		}
	}
}
