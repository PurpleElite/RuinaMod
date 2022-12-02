using System;
using System.Linq;
using UnityEngine;

namespace SeraphDLL
{
    public class DiceCardAbility_seraph_execute : DiceCardAbilityBase
    {
        public static string Desc = "[On Hit] If target is Staggered deal 30 damage";
        private bool _execute = false;
        private string _actionScript;

        public override void BeforeGiveDamage(BattleUnitModel target)
        {
            if (target != null && (target.IsBreakLifeZero() || target.breakDetail.breakGauge == 0))
            {
                _execute = true;
                //behavior.behaviourInCard.ActionScript = behavior.behaviourInCard.ActionScript ?? _actionScript;
            }
        }

        public override void OnSucceedAttack()
        {
            var target = card.target;
            if (_execute)
            {
                _execute = false;
                target.TakeDamage(30, DamageType.Card_Ability, owner);
                BattleCardTotalResult battleCardResultLog = owner.battleCardResultLog;
                if (battleCardResultLog == null)
                {
                    return;
                }
                battleCardResultLog.SetPrintDamagedEffectEvent(new BattleCardBehaviourResult.BehaviourEvent(EarthQuake));
            }
            //else
            //{
            //    _actionScript = behavior.behaviourInCard.ActionScript ?? _actionScript;
            //    behavior.behaviourInCard.ActionScript = null;
            //}
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
            get { return new string[] { "SeraphRunaways_UnstableEntropy" }; }
        }

        public override void OnSucceedAttack(BattleUnitModel target)
        {
            var buffDetail = target.bufListDetail;
            var buff = (BattleUnitBuf_seraph_unstable_entropy)buffDetail.GetActivatedBufList().Find(x => x is BattleUnitBuf_seraph_unstable_entropy);
            
            if (buff != null)
            {
                buff.stack = BattleUnitBuf_seraph_unstable_entropy.Duration + 1;
            }
            else
            {
                target.bufListDetail.AddBuf(new BattleUnitBuf_seraph_unstable_entropy());
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
        delegate void Test(int x);
        
        public static string Desc = "[On Hit] Recycle this die three times. When max value is rolled inflict 1 Erosion to self.";

        private int count;

        public override void OnSucceedAttack(BattleUnitModel target)
        {
            if (behavior.DiceVanillaValue == behavior.GetDiceMax())
            {
                owner.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Decay, 1, owner);
                if (owner.bufListDetail.GetActivatedBuf(KeywordBuf.Decay) is BattleUnitBuf_Decay erosionOwner)
                {
                    erosionOwner.ChangeToYanDecay();
                }
                //if (target != null)
                //{
                //    target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Decay, 1, owner);
                //    if (target.bufListDetail.GetActivatedBuf(KeywordBuf.Decay) is BattleUnitBuf_Decay erosionTarget)
                //    {
                //        erosionTarget.ChangeToYanDecay();
                //    }
                //}
            }
            if (count >= 3)
            {
                return;
            }
            ActivateBonusAttackDice();
            count++;
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

    public class DiceCardAbility_seraph_erosion1 : DiceCardAbilityBase
    {
        public static string Desc = "[On Hit] Inflict target with 1 Erosion if they have Unstable Entropy";

        protected const int Amount = 1;

        public override void OnSucceedAttack(BattleUnitModel target)
        {
            if (target != null && target.bufListDetail.GetActivatedBufList().Any(x => x is BattleUnitBuf_seraph_unstable_entropy entropy))
            {
                target.bufListDetail.AddKeywordBufThisRoundByCard(KeywordBuf.Decay, Amount, owner);
                if (target.bufListDetail.GetActivatedBuf(KeywordBuf.Decay) is BattleUnitBuf_Decay erosion)
                {
                    erosion.ChangeToYanDecay();
                }
            }
        }
    }

    public class DiceCardAbility_seraph_erosion2 : DiceCardAbility_seraph_erosion1
    {
        public static new string Desc = "[On Hit] Inflict target with 2 Erosion if they have Unstable Entropy";
        protected new const int Amount = 2;
    }

    public class DiceCardAbility_seraph_erosion3 : DiceCardAbility_seraph_erosion1
    {
        public static new string Desc = "[On Hit] Inflict target with 3 Erosion if they have Unstable Entropy";
        protected new const int Amount = 3;
    }
}
