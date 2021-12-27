using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomDLLs
{
    public class EnemyTeamStageManager_Seraph : EnemyTeamStageManager
    {
        public override bool HideEnemyTarget()
        {
            var combatants = BattleObjectManager.instance.GetAliveList(Faction.Enemy);
            var linus = combatants.Find(x => x.Book.BookId == new LorId(ModData.WorkshopId, 2));
            var sheireDistracting = combatants.Find(x => x.Book.BookId == new LorId(ModData.WorkshopId, 3)).allyCardDetail.GetUse().Any(x => x.GetID() == new LorId(ModData.WorkshopId, 16));
            if (sheireDistracting)
            {
                
            }
            return base.HideEnemyTarget();
        }
    }
}
