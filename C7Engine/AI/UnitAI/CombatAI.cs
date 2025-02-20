using System.Collections.Generic;
using C7GameData;
using C7GameData.AIData;
using Serilog;

namespace C7Engine.AI.UnitAI {
	/// <summary>
	/// A unit whose intended role is combat, other than purely point-defense combat.
	///
	/// This is likely to evolve significantly, and likely have sub-classes; this is the very first iteration.
	/// This first iteration is focused on defeating barbarians.
	/// </summary>
	public class CombatAI : C7GameData.UnitAI {
		private CombatAIData combatAIData;

		private ILogger log = Log.ForContext<CombatAI>();

		public CombatAI(CombatAIData d) {
			combatAIData = d;
		}

		public bool PlayTurnImpl(Player player, MapUnit unit) {
			//Move along the path to the place where the unit plans to enter combat.
			//This might initiate combat.
			//Once there are more combat goals than run to a barbarian camp,
			//we will need fancier algorithms
			unit.path = combatAIData.path;
			unit.moveAlongPath();
			return true;
		}

		public string SummarizePlan() {
			return "CombateAI: " + combatAIData.ToString();
		}
	}
}
