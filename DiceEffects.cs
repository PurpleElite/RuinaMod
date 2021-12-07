﻿using UnityEngine;

namespace CustomDLLs
{
    public class DiceCardAbility_multi : DiceCardAbilityBase
    {
        public static string Desc = "This die is rolled up to 4 times at the cost of one light per extra roll.";

        private int rerollCount = 0;
        public override void AfterAction()
        {
            if (rerollCount < 3 && owner.cardSlotDetail.PlayPoint > 0)
            {
                Debug.Log("Light count: " + owner.cardSlotDetail.PlayPoint);
                owner.cardSlotDetail.LosePlayPoint(1);
                ActivateBonusAttackDice();
                rerollCount++;
            }
        }
    }

    public class DiceCardAbility_execute : DiceCardAbilityBase
    {
        public static string Desc = "[On Hit] If target is Staggered deal 60 damage";

        public override void OnSucceedAttack()
        {
            var target = card.target;
            Debug.Log("execute OnSuceedAttack target " + target.view.name);
            if (target != null && (target.IsBreakLifeZero() || target.breakDetail.breakGauge == 0))
            {
                Debug.Log("Target break life is zero");
                target.TakeDamage(60, DamageType.Card_Ability, owner);
                BattleCardTotalResult battleCardResultLog = owner.battleCardResultLog;
                if (battleCardResultLog == null)
                {
                    return;
                }
                battleCardResultLog.SetPrintDamagedEffectEvent(new BattleCardBehaviourResult.BehaviourEvent(EarthQuake));
            }
            base.OnSucceedAttack();
        }

        private void EarthQuake()
        {
            FilterUtil.ShowWarpBloodFilter();
            BattleCamManager instance = SingletonBehavior<BattleCamManager>.Instance;
            CameraFilterPack_FX_EarthQuake cameraFilterPack_FX_EarthQuake = ((instance != null) ? instance.EffectCam.gameObject.AddComponent<CameraFilterPack_FX_EarthQuake>() : null) ?? null;
            if (cameraFilterPack_FX_EarthQuake != null)
            {
                cameraFilterPack_FX_EarthQuake.X = 0.075f;
                cameraFilterPack_FX_EarthQuake.Y = 0.01f;
                cameraFilterPack_FX_EarthQuake.Speed = 50f;
            }
            BattleCamManager instance2 = SingletonBehavior<BattleCamManager>.Instance;
            AutoScriptDestruct autoScriptDestruct = ((instance2 != null) ? instance2.EffectCam.gameObject.AddComponent<AutoScriptDestruct>() : null) ?? null;
            if (autoScriptDestruct != null)
            {
                autoScriptDestruct.targetScript = cameraFilterPack_FX_EarthQuake;
                autoScriptDestruct.time = 0.4f;
            }
        }
    }
}