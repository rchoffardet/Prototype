using C7GameData;
using C7GameData.AIData;
using System;

namespace C7GameData {
	//Not-fully-fleshed out player unit AI.
	//Right now it's kind of random to just add some appearance
	//of stuff being done.  I.e. it kinda sucks.  But that's okay.
	//It has to start somewhere, right?
	public interface UnitAI {
		public bool PlayTurn(Player player, MapUnit unit) {
			do {
				bool wasSuccessful = PlayTurnImpl(player, unit);
				if (!wasSuccessful) {
					return false;
				}
			} while (unit.movementPoints.canMove && !unit.isFortified);
			return true;
		}

		// To be implemented by each AI subclass.
		protected abstract bool PlayTurnImpl(Player player, MapUnit unit);

		// Provide a string representation of the current AI plan.
		string SummarizePlan();
	}
}
