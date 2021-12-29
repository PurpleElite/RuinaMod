using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomDLLs
{
	public class DiceCardPriority_test : DiceCardPriorityBase
	{
		public override int GetPriorityBonus(BattleUnitModel owner)
		{
			return 0;
		}

		public virtual int GetPriorityBonus(BattleUnitModel owner, int speed)
		{
			return 0;
		}
	}
}
