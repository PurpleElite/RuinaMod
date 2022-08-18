using System;
using System.Linq;
using UnityEngine;

namespace CustomDLLs
{
    public class DiceCardAbility_seraph_multi : DiceCardAbilityBase
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

    public class DiceCardAbility_seraph_execute : DiceCardAbilityBase
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
            CameraFilterPack_FX_EarthQuake cameraFilterPack_FX_EarthQuake = (SingletonBehavior<BattleCamManager>.Instance?.EffectCam.gameObject.AddComponent<CameraFilterPack_FX_EarthQuake>()) ?? null;
            if (cameraFilterPack_FX_EarthQuake != null)
            {
                cameraFilterPack_FX_EarthQuake.X = 0.075f;
                cameraFilterPack_FX_EarthQuake.Y = 0.01f;
                cameraFilterPack_FX_EarthQuake.Speed = 50f;
            }
            AutoScriptDestruct autoScriptDestruct = (SingletonBehavior<BattleCamManager>.Instance?.EffectCam.gameObject.AddComponent<AutoScriptDestruct>()) ?? null;
            if (autoScriptDestruct != null)
            {
                autoScriptDestruct.targetScript = cameraFilterPack_FX_EarthQuake;
                autoScriptDestruct.time = 0.4f;
            }
        }
    }

    public class DiceCardAbility_seraph_entropy : DiceCardAbilityBase
    {
        public static string Desc = "[On Hit] Inflict Unstable Entropy";

        public override string[] Keywords
        {
            get { return new string[] { "UnstableEntropy" }; }
        }
        public override void OnSucceedAttack(BattleUnitModel target)
        {
            Debug.Log("entropy OnSucceedAttack()");
            var buffDetail = target.bufListDetail;
            var buff = (BattleUnitBuf_unstable_entropy)buffDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_unstable_entropy);
            if (buff != null)
            {
                Debug.Log("entropy setting stack to " + BattleUnitBuf_unstable_entropy.Duration + 1);
                buff.stack = BattleUnitBuf_unstable_entropy.Duration + 1;
            }
            else
            {
                Debug.Log("entropy AddReadyBuf");
                target.bufListDetail.AddReadyBuf(new BattleUnitBuf_unstable_entropy());
            }
        } 
    }

    public class DiceCardAbility_seraph_powerDown3targetAtk : DiceCardAbilityBase
    {
        public static string Desc = "Reduce Power of target's current Offensive die by 3";

        public override void BeforeRollDice()
        {
            if (behavior.TargetDice != null)
            {
                BattleDiceBehavior targetDice = behavior.TargetDice;
                if (IsAttackDice(targetDice.Detail))
                {
                    targetDice.ApplyDiceStatBonus(new DiceStatBonus
                    {
                        power = -3
                    });
                }
            }
        }
    }

    public class DiceCardAbility_seraph_borrowed_time_recycle : DiceCardAbilityBase
    {
        public static string Desc = "[On Hit] Recycle this die three times. When max value is rolled inflict 1 Erosion to target and self.";

        private int count;

        public override void OnSucceedAttack(BattleUnitModel target)
        {
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Decay, 1, owner);
            if (owner.bufListDetail.GetActivatedBuf(KeywordBuf.Decay) is BattleUnitBuf_Decay erosionOwner)
            {
                erosionOwner.ChangeToYanDecay();
            }
            if (target != null)
            {
                target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Decay, 1, owner);
                if (target.bufListDetail.GetActivatedBuf(KeywordBuf.Decay) is BattleUnitBuf_Decay erosionTarget)
                {
                    erosionTarget.ChangeToYanDecay();
                }
            }
            if (count >= 3)
            {
                return;
            }
            if (behavior.DiceVanillaValue == behavior.GetDiceMax())
            {
                ActivateBonusAttackDice();
                count++;
            }
        }
    }

    public class DiceCardAbility_seraph_bonds_powerup : DiceCardAbilityBase
    {
        public static string Desc = "add power equal to stacks of The Bonds that Bind Us, to a max of 4";

        public override void BeforeRollDice()
        {
            var stacks = owner.bufListDetail.GetActivatedBufList().OfType<BattleUnitBuf_seraph_bonds>().Select(x => x.stack).Sum();
            behavior.ApplyDiceStatBonus(new DiceStatBonus { power = Math.Min(stacks, 4) });
        }
    }

    public class DiceCardAbility_seraph_erosion_kickback1 : DiceCardAbilityBase
    {
        public static string Desc = "[On Hit] Inflict target and self with 1 Erosion";

        protected const int Amount = 1;

        public override void OnSucceedAttack(BattleUnitModel target)
        {
            owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Decay, Amount, owner);
            var erosion = owner.bufListDetail.GetActivatedBuf(KeywordBuf.Decay) as BattleUnitBuf_Decay;
            if (erosion != null)
            {
                erosion.ChangeToYanDecay();
            }
            if (target != null)
            {
                target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Decay, Amount, owner);
                erosion = target.bufListDetail.GetActivatedBuf(KeywordBuf.Decay) as BattleUnitBuf_Decay;
                if (erosion != null)
                {
                    erosion.ChangeToYanDecay();
                }
            }
        }
    }

    public class DiceCardAbility_seraph_erosion_kickback2 : DiceCardAbility_seraph_erosion_kickback1
    {
        public static new string Desc = "[On Hit] Inflict target and self with 2 Erosion";
        protected new const int Amount = 2;
    }

    public class DiceCardAbility_seraph_erosion_kickback3 : DiceCardAbility_seraph_erosion_kickback1
    {
        public static new string Desc = "[On Hit] Inflict target and self with 3 Erosion";
        protected new const int Amount = 3;
    }
}
